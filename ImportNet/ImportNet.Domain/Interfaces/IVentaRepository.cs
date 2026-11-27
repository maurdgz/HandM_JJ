using ImportNet.Domain.Entities;

namespace ImportNet.Domain.Interfaces;

public interface IVentaRepository : IGenericRepository<Venta>
{
    /// <summary>
    /// Ventas más recientes primero. <paramref name="desdeUtc"/> es inclusivo y
    /// <paramref name="hastaUtc"/> exclusivo. Con <paramref name="empleadoId"/> solo devuelve sus ventas.
    /// </summary>
    Task<Pagina<ResumenVenta>> Historial(
        DateTime? desdeUtc,
        DateTime? hastaUtc,
        int? empleadoId,
        int pagina,
        int tamano,
        CancellationToken ct);

    /// <summary>Datos de la nota de venta; null si la venta no existe.</summary>
    Task<NotaVenta?> Nota(int ventaId, CancellationToken ct);

    Task<IReadOnlyList<DetalleVenta>> Detalles(int ventaId, CancellationToken ct);
}
