using ImportNet.Domain.Common;
using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportNet.Infrastructure.Repositories;

internal class GenericRepository<T>(AppDbContext context) : IGenericRepository<T> where T : Entity
{
    protected readonly AppDbContext _context = context;
    protected readonly DbSet<T> _dbSet = context.Set<T>();

    public virtual async Task<IReadOnlyList<T>> ListarAsync(CancellationToken ct = default)
        => await _dbSet.AsNoTracking().OrderBy(x => x.Id).ToListAsync(ct);

    public async Task<T?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
        => await _dbSet.FindAsync([id], ct);

    public Task<bool> ExisteAsync(int id, CancellationToken ct = default)
        => _dbSet.AnyAsync(x => x.Id == id, ct);

    public void Agregar(T entidad) => _dbSet.Add(entidad);

    public void Actualizar(T entidad) => _dbSet.Update(entidad);

    public void Eliminar(T entidad) => _dbSet.Remove(entidad);
}
