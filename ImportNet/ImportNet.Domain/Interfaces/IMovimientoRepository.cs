using ImportNet.Domain.Entities;

namespace ImportNet.Domain.Interfaces;

public interface IMovimientoRepository
{
    void Agregar(MovimientoInventario movimiento);
    Task<bool> TieneEntradasDeCompra(int compraId, CancellationToken ct);
    Task<KardexProducto?> Kardex(int productoId, DateTime? desdeUtc, DateTime? hastaUtc, CancellationToken ct);
}
