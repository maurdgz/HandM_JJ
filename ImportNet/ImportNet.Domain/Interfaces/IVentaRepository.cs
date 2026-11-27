using ImportNet.Domain.Entities;

namespace ImportNet.Domain.Interfaces;

public interface IVentaRepository
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
}
