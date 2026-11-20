using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportNet.Infrastructure.Repositories;

internal sealed class InventarioRepository(
    AppDbContext context) : IInventarioRepository
{
    public Task<Inventario?> Obtener(
        int productoId,
        int almacenId,
        CancellationToken ct)
    {
        return context.Set<Inventario>()
            .SingleOrDefaultAsync(
                x => x.ProductoId == productoId
                    && x.AlmacenId == almacenId,
                ct);
    }

    public void Agregar(Inventario inventario)
    {
        context.Set<Inventario>().Add(inventario);
    }
}