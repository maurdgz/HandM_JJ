using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Compras;

public sealed record RecibirCompraCommand(int Id) : IRequest<CompraDto>;

internal sealed class RecibirCompraCommandHandler : IRequestHandler<RecibirCompraCommand, CompraDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUsuarioActual _actor;
    private readonly IEmpleadoRepository _empleados;

    public RecibirCompraCommandHandler(IUnitOfWork unitOfWork, IUsuarioActual actor, IEmpleadoRepository empleados)
    {
        _unitOfWork = unitOfWork;
        _actor = actor;
        _empleados = empleados;
    }

    public async Task<CompraDto> Handle(RecibirCompraCommand request, CancellationToken cancellationToken)
    {
        await _actor.ExigirAdministrador(cancellationToken);

        if (request.Id <= 0)
            throw new ReglaNegocioException("Indique una compra válida.");

        return await _unitOfWork.EjecutarEnTransaccion(async ct =>
        {
            var empleado = await _empleados.ObtenerEmpleadoActivo(_actor, ct);

            var compra = await _unitOfWork.Compras.Obtener(request.Id, ct);
            if (compra == null)
                throw new NoEncontradoException("La compra no existe.");

            if (compra.Estado != EstadoCompra.Borrador)
                throw new ConflictoException("La compra ya fue recibida.");

            if (await _unitOfWork.Movimientos.TieneEntradasDeCompra(compra.Id, ct))
                throw new ConflictoException("La compra ya tiene entradas de inventario.");

            if (compra.Lineas.Count == 0 || compra.Lineas.Count > 100)
                throw new ReglaNegocioException("La compra debe tener entre 1 y 100 líneas.");

            var fechaRecepcion = DateTime.UtcNow;

            foreach (var linea in compra.Lineas.OrderBy(l => l.ProductoId))
            {
                var producto = await _unitOfWork.Productos.Obtener(linea.ProductoId, ct);
                if (producto == null)
                    throw new NoEncontradoException("Un producto de la compra no existe.");

                var inventario = await _unitOfWork.Inventarios.Obtener(producto.Id, Almacen.PrincipalId, ct);
                if (inventario == null)
                {
                    inventario = new Inventario { ProductoId = producto.Id, AlmacenId = Almacen.PrincipalId, Cantidad = 0m };
                    _unitOfWork.Inventarios.Agregar(inventario);
                }

                producto.ActualizarCostoPromedioPorRecepcion(inventario.Cantidad, linea.Cantidad, linea.TotalPen);
                inventario.Cantidad += linea.Cantidad;

                _unitOfWork.Movimientos.Agregar(new MovimientoInventario
                {
                    ProductoId = producto.Id,
                    AlmacenId = Almacen.PrincipalId,
                    Tipo = TipoMovimiento.Entrada,
                    Cantidad = linea.Cantidad,
                    Fecha = fechaRecepcion,
                    EmpleadoId = empleado.Id,
                    ProveedorId = compra.ProveedorId,
                    CompraId = compra.Id
                });
            }

            compra.Recibir();
            return CompraDto.Desde(compra);
        }, cancellationToken);
    }
}
