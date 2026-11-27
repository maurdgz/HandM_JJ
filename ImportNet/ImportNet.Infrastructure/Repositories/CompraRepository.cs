using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportNet.Infrastructure.Repositories;

internal sealed class CompraRepository : ICompraRepository
{
    private readonly AppDbContext _context;

    public CompraRepository(AppDbContext context)
    {
        _context = context;
    }

    public void Agregar(Compra compra)
    {
        _context.Set<Compra>().Add(compra);
    }

    public void MarcarEdicion(Compra compra)
    {
        compra.ExigirBorrador();
        _context.Entry(compra).Property(c => c.TipoCambio).IsModified = true;
    }

    public void Eliminar(Compra compra)
    {
        compra.ExigirBorrador();
        _context.Set<Compra>().Remove(compra);
    }

    public async Task<Pagina<Compra>> Listar(int pagina, int tamano, CancellationToken ct)
    {
        var count = await _context.Set<Compra>().CountAsync(ct);

        var items = await _context.Set<Compra>()
            .AsNoTracking()
            .Include(c => c.Lineas.OrderBy(l => l.Orden))
            .OrderByDescending(c => c.Fecha)
            .ThenByDescending(c => c.Id)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .ToListAsync(ct);

        return new Pagina<Compra>(items, count, pagina, tamano);
    }

    public Task<Compra?> Obtener(int id, CancellationToken ct)
    {
        return _context.Set<Compra>()
            .Include(c => c.Lineas.OrderBy(l => l.Orden))
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }
}
