using ImportNet.Domain.Entities;

namespace ImportNet.Domain.Interfaces;

public interface IReporteRepository
{
    Task<List<VentasPorPeriodo>> VentasPorPeriodoAsync(DateTime desdeUtc, DateTime hastaUtc,
        AgrupacionPeriodo agrupacion, CancellationToken ct = default);
    Task<List<VentasPorVendedor>> VentasPorVendedorAsync(DateTime desdeUtc, DateTime hastaUtc,
        CancellationToken ct = default);
    Task<List<TopProducto>> TopProductosAsync(DateTime desdeUtc, DateTime hastaUtc, int top,
        CancellationToken ct = default);
    Task<List<MargenProducto>> MargenPorProductoAsync(DateTime desdeUtc, DateTime hastaUtc,
        CancellationToken ct = default);
    Task<List<ProductoStockBajo>> StockBajoMinimoAsync(CancellationToken ct = default);
    Task<CierreCaja> CierreCajaAsync(DateOnly fecha, DateTime desdeUtc, DateTime hastaUtc, int? empleadoId,
        CancellationToken ct = default);
    Task<List<ComprasPorProveedor>> ComprasPorProveedorAsync(DateTime desdeUtc, DateTime hastaUtc,
        CancellationToken ct = default);
    Task<ResumenDashboard> DashboardAsync(DateOnly fecha, DateTime inicioDiaUtc, DateTime finDiaUtc,
        DateTime inicioMesUtc, CancellationToken ct = default);
}
