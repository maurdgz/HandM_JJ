using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Compras;

public sealed record RecibirCompraCommand(int Id)
    : IRequest<CompraDto>;

internal sealed class RecibirCompraCommandHandler(
    IUnitOfWork uow,
    IUsuarioActual actor,
    IEmpleadoRepository empleados)
    : IRequestHandler<RecibirCompraCommand, CompraDto>
{
    public async Task<CompraDto> Handle(
        RecibirCompraCommand request,
        CancellationToken ct)
    {
        await actor.ExigirAdministrador(ct);

        var usuarioId = actor.Id
            ?? throw new AccesoNoPermitidoException(401);

        if (request.Id <= 0)
            throw new ReglaNegocioException(
                "Indique una compra válida.");

        return await uow.EjecutarEnTransaccion(
            async token =>
            {
                var empleado =
                    await empleados.ObtenerPorUsuario(
                        usuarioId,
                        token);

                if (empleado is null || !empleado.Activo)
                    throw new AccesoNoPermitidoException(403);

                var compra =
                    await uow.Compras.Obtener(
                        request.Id,
                        token)
                    ?? throw new NoEncontradoException(
                        "La compra no existe.");

                if (compra.Estado != EstadoCompra.Borrador)
                    throw new ConflictoException(
                        "La compra ya fue recibida.");

                if (await uow.Movimientos.TieneEntradasDeCompra(
                    compra.Id,
                    token))
                    throw new ConflictoException(
                        "La compra ya tiene entradas de inventario.");

                if (compra.Lineas.Count == 0
                    || compra.Lineas.Count > 100)
                    throw new ReglaNegocioException(
                        "La compra debe tener entre 1 y 100 líneas.");

                var fechaRecepcion = DateTime.UtcNow;

                foreach (var linea in compra.Lineas
                    .OrderBy(x => x.ProductoId))
                {
                    var producto =
                        await uow.Productos.Obtener(
                            linea.ProductoId,
                            token)
                        ?? throw new NoEncontradoException(
                            "Un producto de la compra no existe.");

                    var inventario =
                        await uow.Inventarios.Obtener(
                            producto.Id,
                            Almacen.PrincipalId,
                            token);

                    if (inventario is null)
                    {
                        inventario = new ImportNet.Domain.Entities.Inventario
                        {
                            ProductoId = producto.Id,
                            AlmacenId = Almacen.PrincipalId,
                            Cantidad = 0m
                        };

                        uow.Inventarios.Agregar(inventario);
                    }

                    var stockAnterior = inventario.Cantidad;

                    producto.ActualizarCostoPromedioPorRecepcion(
                        stockAnterior,
                        linea.Cantidad,
                        linea.TotalPen);

                    inventario.Cantidad =
                        stockAnterior + linea.Cantidad;

                    uow.Movimientos.Agregar(
                        new MovimientoInventario
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
            },
            ct);
    }
}