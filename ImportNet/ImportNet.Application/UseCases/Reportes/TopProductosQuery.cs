using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Exceptions;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Reportes;

public sealed record TopProductosQuery(DateOnly? Desde = null, DateOnly? Hasta = null, int Top = 10) : IRequest<List<TopProducto>>;

internal sealed class TopProductosQueryHandler(IUnitOfWork unitOfWork, IUsuarioActual actor)
    : IRequestHandler<TopProductosQuery, List<TopProducto>>
{
    public async Task<List<TopProducto>> Handle(TopProductosQuery request, CancellationToken cancellationToken)
    {
        await actor.ExigirAdministradorAsync(cancellationToken);

        if (request.Top < 1 || request.Top > 50)
            throw new ReglaNegocioException("El top debe estar entre 1 y 50.");

        var periodo = PeriodoReporte.Desde(request.Desde, request.Hasta);
        return await unitOfWork.Reportes.TopProductosAsync(periodo.DesdeUtc, periodo.HastaUtc, request.Top, cancellationToken);
    }
}
