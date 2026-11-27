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
}
