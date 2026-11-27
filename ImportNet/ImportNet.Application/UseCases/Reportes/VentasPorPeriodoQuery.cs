using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Reportes;

public sealed record VentasPorPeriodoQuery(DateOnly? Desde = null, DateOnly? Hasta = null, string Agrupacion = "Dia")
    : IRequest<List<VentasPorPeriodo>>;

internal sealed class VentasPorPeriodoQueryHandler : IRequestHandler<VentasPorPeriodoQuery, List<VentasPorPeriodo>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUsuarioActual _actor;

    public VentasPorPeriodoQueryHandler(IUnitOfWork unitOfWork, IUsuarioActual actor)
    {
        _unitOfWork = unitOfWork;
        _actor = actor;
    }

    public async Task<List<VentasPorPeriodo>> Handle(VentasPorPeriodoQuery request, CancellationToken cancellationToken)
    {
        await _actor.ExigirAdministrador(cancellationToken);

        AgrupacionPeriodo agrupacion;
        if (string.Equals(request.Agrupacion, "Dia", StringComparison.OrdinalIgnoreCase))
            agrupacion = AgrupacionPeriodo.Dia;
        else if (string.Equals(request.Agrupacion, "Mes", StringComparison.OrdinalIgnoreCase))
            agrupacion = AgrupacionPeriodo.Mes;
        else
            throw new ReglaNegocioException("La agrupación debe ser Dia o Mes.");

        var maximoDias = agrupacion == AgrupacionPeriodo.Mes ? 1100 : 366;
        var periodo = PeriodoReporte.Desde(request.Desde, request.Hasta, maximoDias);

        return await _unitOfWork.Reportes.VentasPorPeriodo(periodo.DesdeUtc, periodo.HastaUtc, agrupacion, cancellationToken);
    }
}
