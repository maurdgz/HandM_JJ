using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportNet.Infrastructure.Repositories;

internal sealed class MovimientoRepository(
    AppDbContext context) : IMovimientoRepository
{
    public void Agregar(MovimientoInventario movimiento)
    {
        context.Set<MovimientoInventario>().Add(movimiento);
    }

    public Task<bool> TieneEntradasDeCompra(
        int compraId,
        CancellationToken ct)
    {
        return context.Set<MovimientoInventario>()
            .AnyAsync(
                x => x.CompraId == compraId
                    && x.Tipo == TipoMovimiento.Entrada,
                ct);
    }

    public async Task<IReadOnlyList<MovimientoInventario>>
        ListarPorCompra(
            int compraId,
            CancellationToken ct)
    {
        return await context.Set<MovimientoInventario>()
            .AsNoTracking()
            .Where(x => x.CompraId == compraId)
            .OrderBy(x => x.Fecha)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);
    }
}