using ImportNet.Domain.Entities;

namespace ImportNet.Domain.Interfaces;

public interface IVentaRepository
{
    Task<Pagina<ResumenVenta>> HistorialAsync(DateTime? desdeUtc, DateTime? hastaUtc, int? empleadoId, int pagina,
        int tamano, CancellationToken ct = default);
    Task<NotaVenta?> ObtenerNotaAsync(int ventaId, CancellationToken ct = default);
    Task<Venta?> ObtenerConDetallesAsync(int ventaId, CancellationToken ct = default);
    Task<int> SiguienteNumeroAsync(CancellationToken ct = default);
    void Agregar(Venta venta);
}
