using ImportNet.Domain.Entities;

namespace ImportNet.Domain.Interfaces;

public interface IMovimientoRepository
{
    void Agregar(MovimientoInventario movimiento);

    Task<bool> TieneEntradasDeCompra(
        int compraId,
        CancellationToken ct);

    Task<IReadOnlyList<MovimientoInventario>> ListarPorCompra(
        int compraId,
        CancellationToken ct);

    /// <summary>
    /// Kardex del almacén principal. <paramref name="desdeUtc"/> es inclusivo y
    /// <paramref name="hastaUtc"/> exclusivo; los movimientos previos forman el saldo inicial.
    /// Devuelve null si el producto no existe.
    /// </summary>
    Task<KardexProducto?> Kardex(
        int productoId,
        DateTime? desdeUtc,
        DateTime? hastaUtc,
        CancellationToken ct);
}