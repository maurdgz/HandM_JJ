using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ImportNet.Infrastructure.Repositories;

internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
        Categorias = new CategoriaRepository(context);
        Productos = new ProductoRepository(context);
    }

    public ICategoriaRepository Categorias { get; }
    public IProductoRepository Productos { get; }

    public async Task<int> Guardar(CancellationToken ct)
    {
        try
        {
            return await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictoException("El registro cambió; recargue e intente de nuevo.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" or "23503" })
        {
            throw new ConflictoException("El registro está duplicado o tiene referencias; recargue los datos.");
        }
    }
}
