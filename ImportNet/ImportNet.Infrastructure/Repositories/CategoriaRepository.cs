using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportNet.Infrastructure.Repositories;

internal sealed class CategoriaRepository : GenericRepository<Categoria>, ICategoriaRepository
{
    public CategoriaRepository(AppDbContext context) : base(context)
    {
    }

    public override async Task<IReadOnlyList<Categoria>> Listar(CancellationToken ct)
    {
        return await _dbSet
            .AsNoTracking()
            .OrderBy(c => c.Nombre)
            .ToListAsync(ct);
    }

    public Task<bool> ExisteNombre(string nombreNormalizado, int excluirId, CancellationToken ct)
    {
        return _dbSet.AnyAsync(c => c.NombreNormalizado == nombreNormalizado && c.Id != excluirId, ct);
    }

    public Task<bool> TieneProductos(int id, CancellationToken ct)
    {
        return _context.Set<Producto>().AnyAsync(p => p.CategoriaId == id, ct);
    }
}
