using ImportNet.Domain.Entities;

namespace ImportNet.Domain.Interfaces;

public interface IVentaRepository
{
    Task<Pagina<ResumenVenta>> Historial(DateTime? desdeUtc, DateTime? hastaUtc, int? empleadoId, int pagina, int tamano, CancellationToken ct);
    Task<NotaVenta?> Nota(int ventaId, CancellationToken ct);
    Task<Venta?> ObtenerConDetalles(int ventaId, CancellationToken ct);
    Task<int> SiguienteNumero(CancellationToken ct);
    void Agregar(Venta venta);
}
