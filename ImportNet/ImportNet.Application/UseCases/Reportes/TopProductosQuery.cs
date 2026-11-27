using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Reportes;

public sealed record TopProductosQuery(DateOnly? Desde = null, DateOnly? Hasta = null, int Top = 10) : IRequest<List<TopProducto>>;

internal sealed class TopProductosQueryHandler : IRequestHandler<TopProductosQuery, List<TopProducto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUsuarioActual _actor;

    public TopProductosQueryHandler(IUnitOfWork unitOfWork, IUsuarioActual actor)
    {
        _unitOfWork = unitOfWork;
        _actor = actor;
    }

    public async Task<List<TopProducto>> Handle(TopProductosQuery request, CancellationToken cancellationToken)
    {
        await _actor.ExigirAdministrador(cancellationToken);

        if (request.Top < 1 || request.Top > 50)
            throw new ReglaNegocioException("El top debe estar entre 1 y 50.");

        var periodo = PeriodoReporte.Desde(request.Desde, request.Hasta);
        return await _unitOfWork.Reportes.TopProductos(periodo.DesdeUtc, periodo.HastaUtc, request.Top, cancellationToken);
    }
}
