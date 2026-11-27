using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportNet.Infrastructure.Repositories;

internal sealed class EmpleadoRepository : IEmpleadoRepository
{
    private readonly AppDbContext _context;

    public EmpleadoRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Empleado?> ObtenerPorUsuario(string usuarioId, CancellationToken ct)
    {
        return _context.Set<Empleado>().AsNoTracking().FirstOrDefaultAsync(e => e.UsuarioId == usuarioId, ct);
    }
}
