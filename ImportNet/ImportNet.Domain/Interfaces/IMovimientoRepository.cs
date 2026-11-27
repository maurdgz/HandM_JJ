using ImportNet.Domain.Entities;

namespace ImportNet.Domain.Interfaces;

public interface IMovimientoRepository
{
    void Agregar(MovimientoInventario movimiento);
    Task<bool> TieneEntradasDeCompraAsync(int compraId, CancellationToken ct = default);
    Task<KardexProducto?> ObtenerKardexAsync(int productoId, DateTime? desdeUtc, DateTime? hastaUtc,
        CancellationToken ct = default);
}
