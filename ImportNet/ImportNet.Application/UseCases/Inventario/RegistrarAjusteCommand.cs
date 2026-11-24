using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Inventario;

/// <param name="Motivo">Merma, Rotura, ConteoFisico o DevolucionProveedor.</param>
/// <param name="Cantidad">
/// ConteoFisico: cantidad contada en el almacén. Otros motivos: cantidad que sale del stock.
/// </param>
public sealed record RegistrarAjusteCommand(
    int ProductoId,
    string Motivo,
    decimal Cantidad,
    string? Observacion)
    : IRequest<AjusteRegistradoDto>;

public sealed record AjusteRegistradoDto(
    int MovimientoId,
    int ProductoId,
    string Motivo,
    decimal Diferencia,
    decimal StockAnterior,
    decimal StockNuevo,
    DateTime Fecha);

internal sealed class RegistrarAjusteCommandHandler(
    IUnitOfWork uow,
    IUsuarioActual actor,
    IEmpleadoRepository empleados)
    : IRequestHandler<RegistrarAjusteCommand, AjusteRegistradoDto>
{
    public async Task<AjusteRegistradoDto> Handle(
        RegistrarAjusteCommand request,
        CancellationToken ct)
    {
        await actor.ExigirAdministrador(ct);

        var usuarioId = actor.Id
            ?? throw new AccesoNoPermitidoException(401);

        if (request.ProductoId <= 0)
            throw new ReglaNegocioException("Indique un producto válido.");

        var motivo = AjusteInventario.Interpretar(request.Motivo);

        return await uow.EjecutarEnTransaccion(
            async token =>
            {
                var empleado = await empleados.ObtenerPorUsuario(usuarioId, token);

                if (empleado is null || !empleado.Activo)
                    throw new AccesoNoPermitidoException(403);

                if (await uow.Productos.Obtener(request.ProductoId, token) is null)
                    throw new NoEncontradoException("El producto no existe.");

                var inventario = await uow.Inventarios.Obtener(
                    request.ProductoId,
                    Almacen.PrincipalId,
                    token);

                var stockAnterior = inventario?.Cantidad ?? 0m;

                var movimiento = AjusteInventario.Crear(
                    request.ProductoId,
                    motivo,
                    request.Cantidad,
                    request.Observacion,
                    stockAnterior,
                    empleado.Id,
                    DateTime.UtcNow);

                if (inventario is null)
                {
                    inventario = new ImportNet.Domain.Entities.Inventario
                    {
                        ProductoId = request.ProductoId,
                        AlmacenId = Almacen.PrincipalId,
                        Cantidad = 0m
                    };

                    uow.Inventarios.Agregar(inventario);
                }

                // La columna de versión rechaza el guardado si otra operación cambió este stock.
                inventario.Cantidad = stockAnterior + movimiento.Cantidad;
                uow.Movimientos.Agregar(movimiento);

                // Guarda dentro de la transacción para devolver el Id del movimiento.
                await uow.Guardar(token);

                return new AjusteRegistradoDto(
                    movimiento.Id,
                    movimiento.ProductoId,
                    motivo.ToString(),
                    movimiento.Cantidad,
                    stockAnterior,
                    inventario.Cantidad,
                    movimiento.Fecha);
            },
            ct);
    }
}
