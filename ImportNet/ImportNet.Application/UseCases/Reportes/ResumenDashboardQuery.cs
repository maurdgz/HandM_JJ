using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Reportes;

public sealed record ResumenDashboardQuery : IRequest<ResumenDashboard>;

internal sealed class ResumenDashboardQueryHandler(IUnitOfWork unitOfWork, IUsuarioActual actor)
    : IRequestHandler<ResumenDashboardQuery, ResumenDashboard>
{
    public async Task<ResumenDashboard> Handle(ResumenDashboardQuery request, CancellationToken cancellationToken)
    {
        await actor.ExigirAdministradorAsync(cancellationToken);

        var hoy = FechasPeru.Hoy();
        var dia = PeriodoReporte.Dia(hoy);
        var inicioMes = FechasPeru.InicioDelDiaUtc(new DateOnly(hoy.Year, hoy.Month, 1));

        return await unitOfWork.Reportes.DashboardAsync(hoy, dia.DesdeUtc, dia.HastaUtc, inicioMes, cancellationToken);
    }
}
