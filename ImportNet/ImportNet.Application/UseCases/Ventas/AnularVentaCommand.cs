using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Ventas;

public sealed record AnularVentaCommand(int VentaId, string Motivo) : IRequest<VentaAnuladaDto>;

public sealed record StockRepuestoDto(int ProductoId, decimal Cantidad, decimal StockNuevo);

public sealed record VentaAnuladaDto(
    int Id,
    int Numero,
    string Estado,
    string Motivo,
    DateTime FechaAnulacion,
    IReadOnlyList<StockRepuestoDto> Reposiciones);

internal sealed class AnularVentaCommandHandler(
    IUnitOfWork uow,
    IUsuarioActual actor,
    IEmpleadoRepository empleados)
    : IRequestHandler<AnularVentaCommand, VentaAnuladaDto>
{
    public async Task<VentaAnuladaDto> Handle(
        AnularVentaCommand request,
        CancellationToken ct)
    {
        await actor.ExigirAdministrador(ct);

        var usuarioId = actor.Id ?? throw new AccesoNoPermitidoException(401);

        if (request.VentaId <= 0)
            throw new NoEncontradoException("La venta no existe.");

        if (string.IsNullOrWhiteSpace(request.Motivo))
            throw new ReglaNegocioException("Indique el motivo de la anulación.");

        return await uow.EjecutarEnTransaccion(
            async token =>
            {
                var empleado = await empleados.ObtenerPorUsuario(usuarioId, token);

                if (empleado is not { Activo: true })
                    throw new AccesoNoPermitidoException(403);

                var venta = await uow.Ventas.Obtener(request.VentaId, token)
                    ?? throw new NoEncontradoException("La venta no existe.");

                if (venta.Estado == EstadoVenta.Anulada)
                    throw new ConflictoException("La venta ya está anulada; el stock no se repone otra vez.");

                venta.Anular(request.Motivo);

                var fecha = venta.FechaAnulacion ?? DateTime.UtcNow;
                var reposiciones = new List<StockRepuestoDto>();

                foreach (var linea in await uow.Ventas.Detalles(venta.Id, token))
                {
                    var inventario = await uow.Inventarios.Obtener(
                        linea.ProductoId,
                        Almacen.PrincipalId,
                        token);

                    if (inventario is null)
                    {
                        inventario = new ImportNet.Domain.Entities.Inventario
                        {
                            ProductoId = linea.ProductoId,
                            AlmacenId = Almacen.PrincipalId,
                            Cantidad = 0m
                        };

                        uow.Inventarios.Agregar(inventario);
                    }

                    // La versión del inventario y de la venta rechaza anulaciones o ventas simultáneas.
                    inventario.Cantidad += linea.Cantidad;

                    // El índice único (VentaId, ProductoId, Almacén, Tipo) impide una segunda entrada.
                    uow.Movimientos.Agregar(new MovimientoInventario
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

                    reposiciones.Add(new StockRepuestoDto(
                        linea.ProductoId,
                        linea.Cantidad,
                        inventario.Cantidad));
                }

                return new VentaAnuladaDto(
                    venta.Id,
                    venta.Numero,
                    venta.Estado.ToString(),
                    venta.MotivoAnulacion!,
                    fecha,
                    reposiciones);
            },
            ct);
    }
}
