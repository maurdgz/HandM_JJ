using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Ventas;

public sealed record LineaVentaEntrada(int ProductoId, decimal Cantidad, decimal Descuento);

public sealed record RegistrarVentaCommand(int? ClienteId, IReadOnlyList<LineaVentaEntrada> Lineas) : IRequest<VentaRegistradaDto>;

public sealed record VentaRegistradaDto(int Id, int Numero, DateTime Fecha, decimal Subtotal, decimal Descuento, decimal Total,
    IReadOnlyList<DetalleVentaDto> Lineas);

public sealed record DetalleVentaDto(int ProductoId, decimal Cantidad, decimal PrecioUnitario, decimal Descuento, decimal Subtotal);

internal sealed class RegistrarVentaCommandHandler : IRequestHandler<RegistrarVentaCommand, VentaRegistradaDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUsuarioActual _usuario;
    private readonly IEmpleadoRepository _empleados;

    public RegistrarVentaCommandHandler(IUnitOfWork unitOfWork, IUsuarioActual usuario, IEmpleadoRepository empleados)
    {
        _unitOfWork = unitOfWork;
        _usuario = usuario;
        _empleados = empleados;
    }

    public async Task<VentaRegistradaDto> Handle(RegistrarVentaCommand request, CancellationToken cancellationToken)
    {
        var empleado = await _empleados.ObtenerEmpleadoActivo(_usuario, cancellationToken);
        var lineas = ValidarLineas(request.Lineas);

        return await _unitOfWork.EjecutarEnTransaccion(async ct =>
        {
            var clienteId = request.ClienteId ?? 1;
            if (await _unitOfWork.Clientes.Cliente(clienteId, ct) == null)
                throw new NoEncontradoException("Cliente no encontrado.");

            var productos = new Dictionary<int, Producto>();
            foreach (var linea in lineas)
            {
                var producto = await _unitOfWork.Productos.Obtener(linea.ProductoId, ct);
                if (producto == null || !producto.Activo)
                    throw new NoEncontradoException("Hay productos inexistentes o inactivos.");

                productos[linea.ProductoId] = producto;
            }

            var inventarios = new Dictionary<int, Inventario>();
            foreach (var linea in lineas)
            {
                if (linea.Descuento > productos[linea.ProductoId].PrecioVenta * linea.Cantidad * 0.5m)
                    throw new ReglaNegocioException("El descuento máximo por línea es 50%.");

                var inventario = await _unitOfWork.Inventarios.Obtener(linea.ProductoId, Almacen.PrincipalId, ct);
                if (inventario == null || inventario.Cantidad < linea.Cantidad)
                    throw new ConflictoException("Stock insuficiente para completar la venta.");

                inventarios[linea.ProductoId] = inventario;
            }

            var subtotal = lineas.Sum(l => decimal.Round(productos[l.ProductoId].PrecioVenta * l.Cantidad, 2, MidpointRounding.AwayFromZero));
            var descuento = lineas.Sum(l => l.Descuento);
            var numero = await _unitOfWork.Ventas.SiguienteNumero(ct);
            var fecha = DateTime.UtcNow;

            var venta = new Venta(numero, empleado.Id, clienteId, subtotal, descuento, fecha);

            foreach (var linea in lineas)
            {
                var producto = productos[linea.ProductoId];
                venta.Detalles.Add(new DetalleVenta(linea.ProductoId, linea.Cantidad, producto.PrecioVenta, linea.Descuento));

                inventarios[linea.ProductoId].Cantidad -= linea.Cantidad;

                _unitOfWork.Movimientos.Agregar(new MovimientoInventario
                {
                    ProductoId = linea.ProductoId,
                    AlmacenId = Almacen.PrincipalId,
                    Tipo = TipoMovimiento.Salida,
                    Cantidad = linea.Cantidad,
                    EmpleadoId = empleado.Id,
                    Venta = venta,
                    Fecha = fecha
                });
            }

            _unitOfWork.Ventas.Agregar(venta);
            await _unitOfWork.Guardar(ct);

            var detalles = venta.Detalles
                .Select(d => new DetalleVentaDto(d.ProductoId, d.Cantidad, d.PrecioUnitario, d.Descuento, d.Subtotal))
                .ToList();

            return new VentaRegistradaDto(venta.Id, venta.Numero, venta.Fecha, venta.Subtotal, venta.Descuento, venta.Total, detalles);
        }, cancellationToken);
    }

    private static List<LineaVentaEntrada> ValidarLineas(IReadOnlyList<LineaVentaEntrada>? lineas)
    {
        if (lineas == null || lineas.Count == 0 || lineas.Count > 100 || lineas.Any(l => l == null))
            throw new ReglaNegocioException("La venta debe tener entre 1 y 100 líneas.");

        foreach (var l in lineas)
        {
            if (l.ProductoId <= 0 || l.Cantidad <= 0 || l.Cantidad > 1000000 || decimal.Round(l.Cantidad, 3) != l.Cantidad
                || l.Descuento < 0 || decimal.Round(l.Descuento, 2) != l.Descuento)
                throw new ReglaNegocioException("Producto, cantidad o descuento inválido.");
        }

        return lineas
            .GroupBy(l => l.ProductoId)
            .Select(g => new LineaVentaEntrada(g.Key, g.Sum(l => l.Cantidad), g.Sum(l => l.Descuento)))
            .ToList();
    }
}
