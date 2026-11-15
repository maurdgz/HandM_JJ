using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using ImportNet.Domain.ReadModels;
using MediatR;

namespace ImportNet.Application.UseCases.Ventas;

internal static class AccesoVentas
{
    public static async Task<Usuario> Usuario(
        IUsuarioActual actor,
        IUsuarioRepository usuarios,
        CancellationToken ct)
    {
        var usuario = actor.Id is { } id
            ? await usuarios.Obtener(id, ct)
            : null;

        if (usuario is null)
            throw new AccesoNoPermitidoException(401);

        return usuario;
    }

    public static int? FiltroEmpleado(Usuario usuario)
    {
        if (usuario.Roles.Any(r =>
            r.Rol.Nombre == RolesImportNet.Administrador))
        {
            return null;
        }

        if (usuario.Roles.Any(r =>
            r.Rol.Nombre == RolesImportNet.Vendedor))
        {
            return usuario.Empleado.Id;
        }

        throw new AccesoNoPermitidoException(403);
    }
}

public sealed record HistorialVentasQuery(
    DateOnly? Desde,
    DateOnly? Hasta) : IRequest<IReadOnlyList<VentaResumen>>;

internal sealed class HistorialVentasQueryHandler(
    IUnitOfWork uow,
    IUsuarioActual actor,
    IUsuarioRepository usuarios)
    : IRequestHandler<
        HistorialVentasQuery,
        IReadOnlyList<VentaResumen>>
{
    public async Task<IReadOnlyList<VentaResumen>> Handle(
        HistorialVentasQuery request,
        CancellationToken ct)
    {
        var usuario = await AccesoVentas.Usuario(
            actor, usuarios, ct);

        var hoyPeru = DateOnly.FromDateTime(
            DateTime.UtcNow.AddHours(-5));

        var desde = request.Desde ?? hoyPeru.AddDays(-30);
        var hasta = request.Hasta ?? hoyPeru;

        if (desde > hasta ||
            hasta == DateOnly.MaxValue ||
            hasta.DayNumber - desde.DayNumber >= 366)
        {
            throw new ReglaNegocioException(
                "Indique un periodo válido de hasta 366 días.");
        }

        var offsetPeru = TimeSpan.FromHours(-5);

        var desdeUtc = new DateTimeOffset(
            desde.ToDateTime(TimeOnly.MinValue),
            offsetPeru).UtcDateTime;

        var hastaUtcExclusivo = new DateTimeOffset(
            hasta.AddDays(1).ToDateTime(TimeOnly.MinValue),
            offsetPeru).UtcDateTime;

        return await uow.Ventas.Historial(
            desdeUtc,
            hastaUtcExclusivo,
            AccesoVentas.FiltroEmpleado(usuario),
            ct);
    }
}

public sealed record GetNotaVentaQuery(int Id)
    : IRequest<NotaVenta>;

internal sealed class GetNotaVentaQueryHandler(
    IUnitOfWork uow,
    IUsuarioActual actor,
    IUsuarioRepository usuarios)
    : IRequestHandler<GetNotaVentaQuery, NotaVenta>
{
    public async Task<NotaVenta> Handle(
        GetNotaVentaQuery request,
        CancellationToken ct)
    {
        if (request.Id <= 0)
        {
            throw new ReglaNegocioException(
                "Identificador inválido.");
        }

        var usuario = await AccesoVentas.Usuario(
            actor, usuarios, ct);

        return await uow.Ventas.Nota(
            request.Id,
            AccesoVentas.FiltroEmpleado(usuario),
            ct)
            ?? throw new NoEncontradoException(
                "Venta no encontrada.");
    }
}