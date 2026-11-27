using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportNet.Infrastructure.Repositories;

internal sealed class CategoriaRepository(AppDbContext context)
    : GenericRepository<Categoria>(context), ICategoriaRepository
{
    public override async Task<IReadOnlyList<Categoria>> ListarAsync(CancellationToken ct = default)
        => await _dbSet.AsNoTracking().OrderBy(c => c.Nombre).ToListAsync(ct);

    public Task<bool> ExisteNombreAsync(string nombreNormalizado, int excluirId, CancellationToken ct = default)
        => _dbSet.AnyAsync(c => c.NombreNormalizado == nombreNormalizado && c.Id != excluirId, ct);

    public Task<bool> TieneProductosAsync(int id, CancellationToken ct = default)
        => _context.Set<Producto>().AnyAsync(p => p.CategoriaId == id, ct);
}
