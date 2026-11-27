using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportNet.Infrastructure.Repositories;

internal sealed class CompraRepository(AppDbContext context)
    : GenericRepository<Compra>(context), ICompraRepository
{
    public void MarcarEdicion(Compra compra)
    {
        compra.ExigirBorrador();
        _context.Entry(compra).Property(c => c.TipoCambio).IsModified = true;
    }

    public new void Eliminar(Compra compra)
    {
        compra.ExigirBorrador();
        base.Eliminar(compra);
    }

    public async Task<Pagina<Compra>> ListarAsync(int pagina, int tamano, CancellationToken ct = default)
    {
        var count = await _dbSet.CountAsync(ct);

        var items = await _dbSet
            .AsNoTracking()
            .Include(c => c.Lineas.OrderBy(l => l.Orden))
            .OrderByDescending(c => c.Fecha)
            .ThenByDescending(c => c.Id)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .ToListAsync(ct);

        return new Pagina<Compra>(items, count, pagina, tamano);
    }

    public Task<Compra?> ObtenerConLineasAsync(int id, CancellationToken ct = default)
        => _dbSet
            .Include(c => c.Lineas.OrderBy(l => l.Orden))
            .FirstOrDefaultAsync(c => c.Id == id, ct);
}
