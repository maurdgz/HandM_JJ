using ImportNet.Domain.Entities;

namespace ImportNet.Domain.Interfaces;

/// <summary>
/// Consultas de reportes. Solo cuentan ventas Registradas. <c>desdeUtc</c> es inclusivo y
/// <c>hastaUtc</c> exclusivo.
/// </summary>
public interface IReporteRepository
{
    Task<IReadOnlyList<VentasPorPeriodo>> VentasPorPeriodo(
        DateTime desdeUtc,
        DateTime hastaUtc,
        AgrupacionPeriodo agrupacion,
        CancellationToken ct);

    Task<IReadOnlyList<VentasPorVendedor>> VentasPorVendedor(
        DateTime desdeUtc,
        DateTime hastaUtc,
        CancellationToken ct);

    /// <summary>Productos más vendidos por cantidad.</summary>
    Task<IReadOnlyList<TopProducto>> TopProductos(
        DateTime desdeUtc,
        DateTime hastaUtc,
        int top,
        CancellationToken ct);

    /// <summary>Margen por producto vendido, de mayor a menor margen.</summary>
    Task<IReadOnlyList<MargenProducto>> MargenPorProducto(
        DateTime desdeUtc,
        DateTime hastaUtc,
        CancellationToken ct);

    /// <summary>Productos activos con stock menor o igual al mínimo, por mayor faltante.</summary>
    Task<IReadOnlyList<ProductoStockBajo>> StockBajoMinimo(CancellationToken ct);

    /// <summary>Con <paramref name="empleadoId"/> solo considera las ventas de ese empleado.</summary>
    Task<CierreCaja> CierreCaja(
        DateOnly fecha,
        DateTime desdeUtc,
        DateTime hastaUtc,
        int? empleadoId,
        CancellationToken ct);

    Task<IReadOnlyList<ComprasPorProveedor>> ComprasPorProveedor(
        DateTime desdeUtc,
        DateTime hastaUtc,
        CancellationToken ct);
}
