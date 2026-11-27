using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Reportes;

/// <param name="Agrupacion">Dia o Mes.</param>
public sealed record VentasPorPeriodoQuery(
    DateOnly? Desde = null,
    DateOnly? Hasta = null,
    string Agrupacion = "Dia")
    : IRequest<IReadOnlyList<VentasPorPeriodo>>;

public sealed record VentasPorVendedorQuery(
    DateOnly? Desde = null,
    DateOnly? Hasta = null)
    : IRequest<IReadOnlyList<VentasPorVendedor>>;

internal sealed class VentasPorPeriodoQueryHandler(IUnitOfWork uow, IUsuarioActual actor)
    : IRequestHandler<VentasPorPeriodoQuery, IReadOnlyList<VentasPorPeriodo>>
{
    public async Task<IReadOnlyList<VentasPorPeriodo>> Handle(VentasPorPeriodoQuery request, CancellationToken ct)
    {
        await actor.ExigirAdministrador(ct);

        if (!Enum.TryParse<AgrupacionPeriodo>(request.Agrupacion, ignoreCase: true, out var agrupacion)
            || !Enum.IsDefined(agrupacion))
            throw new ReglaNegocioException("La agrupación debe ser Dia o Mes.");

        // Por mes se permite un rango mayor para comparar varios años.
        var periodo = PeriodoReporte.Desde(
            request.Desde,
            request.Hasta,
            agrupacion == AgrupacionPeriodo.Mes ? 1100 : 366);

        return await uow.Reportes.VentasPorPeriodo(periodo.DesdeUtc, periodo.HastaUtc, agrupacion, ct);
    }
}

internal sealed class VentasPorVendedorQueryHandler(IUnitOfWork uow, IUsuarioActual actor)
    : IRequestHandler<VentasPorVendedorQuery, IReadOnlyList<VentasPorVendedor>>
{
    public async Task<IReadOnlyList<VentasPorVendedor>> Handle(VentasPorVendedorQuery request, CancellationToken ct)
    {
        await actor.ExigirAdministrador(ct);
        var periodo = PeriodoReporte.Desde(request.Desde, request.Hasta);
        return await uow.Reportes.VentasPorVendedor(periodo.DesdeUtc, periodo.HastaUtc, ct);
    }
}
