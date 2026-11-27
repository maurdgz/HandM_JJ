using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Ventas;

/// <summary>Fechas en hora de Perú (UTC−5), ambas inclusivas.</summary>
public sealed record ConsultarVentasQuery(
    DateOnly? Desde = null,
    DateOnly? Hasta = null,
    int Pagina = 1,
    int Tamano = 20)
    : IRequest<Pagina<ResumenVenta>>;

internal sealed class ConsultarVentasQueryHandler(
    IUnitOfWork uow,
    IUsuarioActual actor,
    IEmpleadoRepository empleados)
    : IRequestHandler<ConsultarVentasQuery, Pagina<ResumenVenta>>
{
    public async Task<Pagina<ResumenVenta>> Handle(
        ConsultarVentasQuery request,
        CancellationToken ct)
    {
        if (request.Pagina < 1 || request.Pagina > 1000000 || request.Tamano < 1 || request.Tamano > 100)
            throw new ReglaNegocioException("Paginación inválida.");

        if (request.Desde > request.Hasta)
            throw new ReglaNegocioException(
                "La fecha inicial no puede ser posterior a la final.");

        var usuarioId = actor.Id ?? throw new AccesoNoPermitidoException(401);
        var empleado = await empleados.ObtenerPorUsuario(usuarioId, ct);

        if (empleado is not { Activo: true })
            throw new AccesoNoPermitidoException(403);

        // El Administrador ve todas las ventas; el Vendedor solo las suyas.
        int? filtroEmpleado = await actor.EsAdministrador(ct) ? null : empleado.Id;

        var desdeUtc = request.Desde is { } desde ? FechasPeru.InicioDelDiaUtc(desde) : (DateTime?)null;
        var hastaUtc = request.Hasta is { } hasta ? FechasPeru.InicioDelDiaUtc(hasta.AddDays(1)) : (DateTime?)null;

        return await uow.Ventas.Historial(
            desdeUtc,
            hastaUtc,
            filtroEmpleado,
            request.Pagina,
            request.Tamano,
            ct);
    }
}
