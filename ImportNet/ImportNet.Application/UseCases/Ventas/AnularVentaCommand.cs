using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Ventas;

public sealed record AnularVentaCommand(int VentaId, string Motivo) : IRequest<VentaAnuladaDto>;

public sealed record StockRepuestoDto(int ProductoId, decimal Cantidad, decimal StockNuevo);

public sealed record VentaAnuladaDto(int Id, int Numero, string Estado, string Motivo, DateTime FechaAnulacion,
    IReadOnlyList<StockRepuestoDto> Reposiciones);

internal sealed class AnularVentaCommandHandler : IRequestHandler<AnularVentaCommand, VentaAnuladaDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUsuarioActual _actor;
    private readonly IEmpleadoRepository _empleados;

    public AnularVentaCommandHandler(IUnitOfWork unitOfWork, IUsuarioActual actor, IEmpleadoRepository empleados)
    {
        _unitOfWork = unitOfWork;
        _actor = actor;
        _empleados = empleados;
    }

    public async Task<VentaAnuladaDto> Handle(AnularVentaCommand request, CancellationToken cancellationToken)
    {
        await _actor.ExigirAdministrador(cancellationToken);

        if (request.VentaId <= 0)
            throw new NoEncontradoException("La venta no existe.");

        if (string.IsNullOrWhiteSpace(request.Motivo))
            throw new ReglaNegocioException("Indique el motivo de la anulación.");

        return await _unitOfWork.EjecutarEnTransaccion(async ct =>
        {
            var empleado = await _empleados.ObtenerEmpleadoActivo(_actor, ct);

            var venta = await _unitOfWork.Ventas.ObtenerConDetalles(request.VentaId, ct);
            if (venta == null)
                throw new NoEncontradoException("La venta no existe.");

            if (venta.Estado == EstadoVenta.Anulada)
                throw new ConflictoException("La venta ya está anulada; el stock no se repone otra vez.");

            venta.Anular(request.Motivo);
            var fecha = venta.FechaAnulacion!.Value;
            var reposiciones = new List<StockRepuestoDto>();

            foreach (var linea in venta.Detalles)
            {
                var inventario = await _unitOfWork.Inventarios.Obtener(linea.ProductoId, Almacen.PrincipalId, ct);
                if (inventario == null)
                {
                    inventario = new Inventario { ProductoId = linea.ProductoId, AlmacenId = Almacen.PrincipalId, Cantidad = 0m };
                    _unitOfWork.Inventarios.Agregar(inventario);
                }

                inventario.Cantidad += linea.Cantidad;

                _unitOfWork.Movimientos.Agregar(new MovimientoInventario
                {
                    ProductoId = linea.ProductoId,
                    AlmacenId = Almacen.PrincipalId,
                    Tipo = TipoMovimiento.Entrada,
                    Cantidad = linea.Cantidad,
                    Fecha = fecha,
                    EmpleadoId = empleado.Id,
                    VentaId = venta.Id,
                    Observacion = venta.MotivoAnulacion
                });

                reposiciones.Add(new StockRepuestoDto(linea.ProductoId, linea.Cantidad, inventario.Cantidad));
            }

            return new VentaAnuladaDto(venta.Id, venta.Numero, venta.Estado.ToString(), venta.MotivoAnulacion!, fecha, reposiciones);
        }, cancellationToken);
    }
}
