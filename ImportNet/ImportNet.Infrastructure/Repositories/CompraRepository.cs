using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace ImportNet.Infrastructure.Repositories;
internal sealed class CompraRepository(AppDbContext db) : ICompraRepository
{
    public async Task<Pagina<Compra>> Listar(int pagina, int tamano, CancellationToken ct)
    {
        var q = db.Set<Compra>().AsNoTracking(); var count = await q.CountAsync(ct);
        var items = await q.Include(x => x.Lineas.OrderBy(l => l.Orden)).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Id).Skip((pagina - 1) * tamano).Take(tamano).ToListAsync(ct);
        return new(items, count, pagina, tamano);
    }
    public Task<Compra?> Obtener(int id, CancellationToken ct) => db.Set<Compra>().Include(x => x.Lineas.OrderBy(l => l.Orden)).SingleOrDefaultAsync(x => x.Id == id, ct);
}
