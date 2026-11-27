using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Ventas;

public sealed record ObtenerNotaVentaQuery(int VentaId) : IRequest<NotaVenta>;

internal sealed class ObtenerNotaVentaQueryHandler(
    IUnitOfWork uow,
    IUsuarioActual actor,
    IEmpleadoRepository empleados)
    : IRequestHandler<ObtenerNotaVentaQuery, NotaVenta>
{
    public async Task<NotaVenta> Handle(
        ObtenerNotaVentaQuery request,
        CancellationToken ct)
    {
        var usuarioId = actor.Id ?? throw new AccesoNoPermitidoException(401);
        var empleado = await empleados.ObtenerPorUsuario(usuarioId, ct);

        if (empleado is not { Activo: true })
            throw new AccesoNoPermitidoException(403);

        if (request.VentaId <= 0)
            throw new NoEncontradoException("La venta no existe.");

        var nota = await uow.Ventas.Nota(request.VentaId, ct)
            ?? throw new NoEncontradoException("La venta no existe.");

        // El Vendedor no puede ver ventas ajenas.
        if (nota.EmpleadoId != empleado.Id && !await actor.EsAdministrador(ct))
            throw new AccesoNoPermitidoException(403);

        return nota;
    }
}
