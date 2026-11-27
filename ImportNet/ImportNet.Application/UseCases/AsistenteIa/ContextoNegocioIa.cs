using ImportNet.Domain.Entities;

namespace ImportNet.Application.UseCases.AsistenteIa;

public sealed record ContextoNegocioIa(
    DateOnly Desde,
    DateOnly Hasta,
    ResumenDashboard Dashboard,
    IReadOnlyList<TopProducto> TopProductos,
    IReadOnlyList<MargenProducto> MargenProductos,
    IReadOnlyList<ProductoStockBajo> StockBajo);
