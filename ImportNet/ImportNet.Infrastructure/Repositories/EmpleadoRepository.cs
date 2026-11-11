using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportNet.Infrastructure.Repositories;

internal sealed class EmpleadoRepository(AppDbContext db) : IEmpleadoRepository
{
    public Task<Empleado?> ObtenerPorUsuario(string usuarioId, CancellationToken ct)
        => db.Set<Empleado>().AsNoTracking().SingleOrDefaultAsync(e => e.UsuarioId == usuarioId, ct);
}
