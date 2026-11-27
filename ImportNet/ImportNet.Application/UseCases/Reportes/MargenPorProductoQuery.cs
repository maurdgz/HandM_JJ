using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Reportes;

public sealed record MargenPorProductoQuery(DateOnly? Desde = null, DateOnly? Hasta = null) : IRequest<List<MargenProducto>>;

internal sealed class MargenPorProductoQueryHandler(IUnitOfWork unitOfWork, IUsuarioActual actor)
    : IRequestHandler<MargenPorProductoQuery, List<MargenProducto>>
{
    public async Task<List<MargenProducto>> Handle(MargenPorProductoQuery request, CancellationToken cancellationToken)
    {
        await actor.ExigirAdministradorAsync(cancellationToken);
        var periodo = PeriodoReporte.Desde(request.Desde, request.Hasta);
        return await unitOfWork.Reportes.MargenPorProductoAsync(periodo.DesdeUtc, periodo.HastaUtc, cancellationToken);
    }
}
