using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Reportes;

public sealed record MargenPorProductoQuery(DateOnly? Desde = null, DateOnly? Hasta = null) : IRequest<List<MargenProducto>>;

internal sealed class MargenPorProductoQueryHandler : IRequestHandler<MargenPorProductoQuery, List<MargenProducto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUsuarioActual _actor;

    public MargenPorProductoQueryHandler(IUnitOfWork unitOfWork, IUsuarioActual actor)
    {
        _unitOfWork = unitOfWork;
        _actor = actor;
    }

    public async Task<List<MargenProducto>> Handle(MargenPorProductoQuery request, CancellationToken cancellationToken)
    {
        await _actor.ExigirAdministrador(cancellationToken);
        var periodo = PeriodoReporte.Desde(request.Desde, request.Hasta);
        return await _unitOfWork.Reportes.MargenPorProducto(periodo.DesdeUtc, periodo.HastaUtc, cancellationToken);
    }
}
