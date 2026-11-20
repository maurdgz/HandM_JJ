using ImportNet.Domain.Entities;

namespace ImportNet.Domain.Interfaces;

public interface IInventarioRepository
{
    Task<Inventario?> Obtener(
        int productoId,
        int almacenId,
        CancellationToken ct);

    void Agregar(Inventario inventario);

    Task<Pagina<StockProducto>> Buscar(
        string? texto,
        bool stockBajo,
        int pagina,
        int tamano,
        CancellationToken ct);
}