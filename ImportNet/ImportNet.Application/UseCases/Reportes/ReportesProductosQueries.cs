using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Reportes;

public sealed record TopProductosQuery(
    DateOnly? Desde = null,
    DateOnly? Hasta = null,
    int Top = 10)
    : IRequest<IReadOnlyList<TopProducto>>;

public sealed record MargenPorProductoQuery(
    DateOnly? Desde = null,
    DateOnly? Hasta = null)
    : IRequest<IReadOnlyList<MargenProducto>>;

internal sealed class TopProductosQueryHandler(IUnitOfWork uow, IUsuarioActual actor)
    : IRequestHandler<TopProductosQuery, IReadOnlyList<TopProducto>>
{
    public async Task<IReadOnlyList<TopProducto>> Handle(TopProductosQuery request, CancellationToken ct)
    {
        await actor.ExigirAdministrador(ct);

        if (request.Top is < 1 or > 50)
            throw new ReglaNegocioException("El top debe estar entre 1 y 50.");

        var periodo = PeriodoReporte.Desde(request.Desde, request.Hasta);
        return await uow.Reportes.TopProductos(periodo.DesdeUtc, periodo.HastaUtc, request.Top, ct);
    }
}

internal sealed class MargenPorProductoQueryHandler(IUnitOfWork uow, IUsuarioActual actor)
    : IRequestHandler<MargenPorProductoQuery, IReadOnlyList<MargenProducto>>
{
    public async Task<IReadOnlyList<MargenProducto>> Handle(MargenPorProductoQuery request, CancellationToken ct)
    {
        await actor.ExigirAdministrador(ct);
        var periodo = PeriodoReporte.Desde(request.Desde, request.Hasta);
        return await uow.Reportes.MargenPorProducto(periodo.DesdeUtc, periodo.HastaUtc, ct);
    }
}
