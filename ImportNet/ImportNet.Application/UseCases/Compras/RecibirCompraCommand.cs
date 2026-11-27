using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Exceptions;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Compras;

public sealed record RecibirCompraCommand(int Id) : IRequest<CompraDto>;

internal sealed class RecibirCompraCommandHandler(IUnitOfWork unitOfWork, IUsuarioActual actor)
    : IRequestHandler<RecibirCompraCommand, CompraDto>
{
    public async Task<CompraDto> Handle(RecibirCompraCommand request, CancellationToken cancellationToken)
    {
        await actor.ExigirAdministradorAsync(cancellationToken);

        if (request.Id <= 0)
            throw new ReglaNegocioException("Indique una compra válida.");

        return await unitOfWork.EjecutarEnTransaccionAsync(async ct =>
        {
            var empleado = await unitOfWork.Empleados.ObtenerEmpleadoActivoAsync(actor, ct);

            var compra = await unitOfWork.Compras.ObtenerConLineasAsync(request.Id, ct)
                ?? throw new NoEncontradoException("La compra no existe.");

            if (compra.Estado != EstadoCompra.Borrador)
                throw new ConflictoException("La compra ya fue recibida.");

            if (await unitOfWork.Movimientos.TieneEntradasDeCompraAsync(compra.Id, ct))
                throw new ConflictoException("La compra ya tiene entradas de inventario.");

            if (compra.Lineas.Count == 0 || compra.Lineas.Count > 100)
                throw new ReglaNegocioException("La compra debe tener entre 1 y 100 líneas.");

            var productoIds = compra.Lineas.Select(l => l.ProductoId).Distinct().ToList();
            var productos = await unitOfWork.Productos.ObtenerPorIdsAsync(productoIds, ct);
            var inventarios = await unitOfWork.Inventarios.ObtenerPorProductosAsync(productoIds, Almacen.PrincipalId, ct);

            var fechaRecepcion = DateTime.UtcNow;

            foreach (var linea in compra.Lineas.OrderBy(l => l.ProductoId))
            {
                if (!productos.TryGetValue(linea.ProductoId, out var producto))
                    throw new NoEncontradoException("Un producto de la compra no existe.");

                if (!inventarios.TryGetValue(producto.Id, out var inventario))
                {
                    inventario = new Inventario
                    {
                        ProductoId = producto.Id,
                        AlmacenId = Almacen.PrincipalId,
                        Cantidad = 0m
                    };
                    unitOfWork.Inventarios.Agregar(inventario);
                }

                producto.ActualizarCostoPromedioPorRecepcion(inventario.Cantidad, linea.Cantidad, linea.TotalPen);
                inventario.Cantidad += linea.Cantidad;

                unitOfWork.Movimientos.Agregar(new MovimientoInventario
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
