using ImportNet.Domain.Entities;

namespace ImportNet.Domain.Interfaces;

public interface IInventarioRepository : IGenericRepository<Inventario>
{
    Task<Inventario?> ObtenerAsync(int productoId, int almacenId, CancellationToken ct = default);
    Task<IReadOnlyDictionary<int, Inventario>> ObtenerPorProductosAsync(IReadOnlyCollection<int> productoIds, int almacenId,
        CancellationToken ct = default);
    Task<Pagina<StockProducto>> BuscarAsync(string? texto, bool stockBajo, int pagina, int tamano,
        CancellationToken ct = default);
}
