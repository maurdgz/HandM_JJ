using ImportNet.Domain.Entities;

namespace ImportNet.Domain.Interfaces;

public interface IReporteRepository
{
    Task<List<VentasPorPeriodo>> VentasPorPeriodo(DateTime desdeUtc, DateTime hastaUtc, AgrupacionPeriodo agrupacion, CancellationToken ct);
    Task<List<VentasPorVendedor>> VentasPorVendedor(DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct);
    Task<List<TopProducto>> TopProductos(DateTime desdeUtc, DateTime hastaUtc, int top, CancellationToken ct);
    Task<List<MargenProducto>> MargenPorProducto(DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct);
    Task<List<ProductoStockBajo>> StockBajoMinimo(CancellationToken ct);
    Task<CierreCaja> CierreCaja(DateOnly fecha, DateTime desdeUtc, DateTime hastaUtc, int? empleadoId, CancellationToken ct);
    Task<List<ComprasPorProveedor>> ComprasPorProveedor(DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct);
    Task<ResumenDashboard> Dashboard(DateOnly fecha, DateTime inicioDiaUtc, DateTime finDiaUtc, DateTime inicioMesUtc, CancellationToken ct);
}
