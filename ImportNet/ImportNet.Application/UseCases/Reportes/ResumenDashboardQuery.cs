using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Reportes;

public sealed record ResumenDashboardQuery : IRequest<ResumenDashboard>;

internal sealed class ResumenDashboardQueryHandler : IRequestHandler<ResumenDashboardQuery, ResumenDashboard>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUsuarioActual _actor;

    public ResumenDashboardQueryHandler(IUnitOfWork unitOfWork, IUsuarioActual actor)
    {
        _unitOfWork = unitOfWork;
        _actor = actor;
    }

    public async Task<ResumenDashboard> Handle(ResumenDashboardQuery request, CancellationToken cancellationToken)
    {
        await _actor.ExigirAdministrador(cancellationToken);

        var hoy = FechasPeru.Hoy();
        var dia = PeriodoReporte.Dia(hoy);
        var inicioMes = FechasPeru.InicioDelDiaUtc(new DateOnly(hoy.Year, hoy.Month, 1));

        return await _unitOfWork.Reportes.Dashboard(hoy, dia.DesdeUtc, dia.HastaUtc, inicioMes, cancellationToken);
    }
}
