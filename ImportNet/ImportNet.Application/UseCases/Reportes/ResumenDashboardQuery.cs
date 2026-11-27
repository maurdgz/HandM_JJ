using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Reportes;

public sealed record ResumenDashboardQuery : IRequest<ResumenDashboard>;

internal sealed class ResumenDashboardQueryHandler(IDashboardRepository dashboard, IUsuarioActual actor)
    : IRequestHandler<ResumenDashboardQuery, ResumenDashboard>
{
    public async Task<ResumenDashboard> Handle(ResumenDashboardQuery request, CancellationToken ct)
    {
        await actor.ExigirAdministrador(ct);
        var hoy = PeriodoReporte.Hoy();
        var dia = PeriodoReporte.Dia(hoy);
        var mes = PeriodoReporte.Desde(new DateOnly(hoy.Year, hoy.Month, 1), hoy);
        return await dashboard.Resumen(hoy, dia.DesdeUtc, dia.HastaUtc, mes.DesdeUtc, ct);
    }
}
