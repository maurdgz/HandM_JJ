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
}