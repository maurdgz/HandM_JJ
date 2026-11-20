using ImportNet.Domain.Entities;

namespace ImportNet.Domain.Interfaces;

public interface IInventarioRepository
{
    Task<Inventario?> Obtener(
        int productoId,
        int almacenId,
        CancellationToken ct);

    void Agregar(Inventario inventario);
}