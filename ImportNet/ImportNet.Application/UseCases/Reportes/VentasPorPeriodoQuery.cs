using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Exceptions;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Reportes;

public sealed record VentasPorPeriodoQuery(DateOnly? Desde = null, DateOnly? Hasta = null, string Agrupacion = "Dia")
    : IRequest<List<VentasPorPeriodo>>;

internal sealed class VentasPorPeriodoQueryHandler(IUnitOfWork unitOfWork, IUsuarioActual actor)
    : IRequestHandler<VentasPorPeriodoQuery, List<VentasPorPeriodo>>
{
    public async Task<List<VentasPorPeriodo>> Handle(VentasPorPeriodoQuery request, CancellationToken cancellationToken)
    {
        await actor.ExigirAdministradorAsync(cancellationToken);

        AgrupacionPeriodo agrupacion;
        if (string.Equals(request.Agrupacion, "Dia", StringComparison.OrdinalIgnoreCase))
            agrupacion = AgrupacionPeriodo.Dia;
        else if (string.Equals(request.Agrupacion, "Mes", StringComparison.OrdinalIgnoreCase))
            agrupacion = AgrupacionPeriodo.Mes;
        else
            throw new ReglaNegocioException("La agrupación debe ser Dia o Mes.");

        var maximoDias = agrupacion == AgrupacionPeriodo.Mes ? 1100 : 366;
        var periodo = PeriodoReporte.Desde(request.Desde, request.Hasta, maximoDias);

        return await unitOfWork.Reportes.VentasPorPeriodoAsync(periodo.DesdeUtc, periodo.HastaUtc, agrupacion, cancellationToken);
    }
}
