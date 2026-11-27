using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportNet.Infrastructure.Repositories;

internal sealed class EmpleadoRepository(AppDbContext context) : IEmpleadoRepository
{
    public Task<Empleado?> ObtenerPorUsuarioAsync(string usuarioId, CancellationToken ct = default)
        => context.Set<Empleado>()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.UsuarioId == usuarioId, ct);
}
