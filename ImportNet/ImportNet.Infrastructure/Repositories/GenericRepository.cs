using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportNet.Infrastructure.Repositories;

internal class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public GenericRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<IReadOnlyList<T>> Listar(CancellationToken ct)
    {
        return await _dbSet
            .AsNoTracking()
            .OrderBy(x => EF.Property<int>(x, "Id"))
            .ToListAsync(ct);
    }

    public async Task<T?> Obtener(int id, CancellationToken ct)
    {
        return await _dbSet.FindAsync([id], ct);
    }

    public Task<bool> Existe(int id, CancellationToken ct)
    {
        return _dbSet.AnyAsync(x => EF.Property<int>(x, "Id") == id, ct);
    }

    public void Agregar(T entidad)
    {
        _dbSet.Add(entidad);
    }

    public void Eliminar(T entidad)
    {
        _dbSet.Remove(entidad);
    }
}
