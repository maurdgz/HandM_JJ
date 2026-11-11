using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ImportNet.Infrastructure.Identity;

internal sealed class UsuarioRepository(AppDbContext db, UserManager<ApplicationUser> users, IEmpleadoRepository empleados)
    : IUsuarioRepository
{
    public async Task<Usuario?> Autenticar(string email, string password, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(email);
        if (user is null || await users.IsLockedOutAsync(user)) return null;
        var empleado = await empleados.ObtenerPorUsuario(user.Id, ct);
        if (empleado?.Activo != true) return null;
        // CheckPasswordAsync rehashes only after successful legacy verification.
        if (!await users.CheckPasswordAsync(user, password))
        {
            await users.AccessFailedAsync(user);
            return null;
        }
        if (!(await users.ResetAccessFailedCountAsync(user)).Succeeded) return null;
        return await Obtener(user.Id, ct);
    }

    public async Task<Usuario?> Obtener(string id, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id, ct);
        var empleado = await empleados.ObtenerPorUsuario(id, ct);
        if (user is null || empleado?.Activo != true || user.SecurityStamp is null
            || (user.LockoutEnabled && user.LockoutEnd > DateTimeOffset.UtcNow)) return null;
        var assigned = await (from ur in db.UserRoles.AsNoTracking()
            join r in db.Roles.AsNoTracking() on ur.RoleId equals r.Id
            where ur.UserId == id select new { r.Id, r.Name }).ToListAsync(ct);
        if (assigned.Count == 0 || assigned.Any(r => !RolesImportNet.Todos.Contains(r.Name!))) return null;
        return new Usuario(user.Id, user.Email!, user.SecurityStamp, empleado,
            assigned.Select(r => new UsuarioRol(id, new Rol(r.Id, r.Name!))).ToArray());
    }
}
