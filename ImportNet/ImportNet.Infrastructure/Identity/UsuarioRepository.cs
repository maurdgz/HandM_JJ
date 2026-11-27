using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace ImportNet.Infrastructure.Identity;

internal sealed class UsuarioRepository(
    UserManager<ApplicationUser> users,
    IEmpleadoRepository empleados) : IUsuarioRepository
{
    public async Task<Usuario?> AutenticarAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await users.FindByEmailAsync(email);
        if (user == null || await users.IsLockedOutAsync(user))
            return null;

        var empleado = await empleados.ObtenerPorUsuarioAsync(user.Id, ct);
        if (empleado == null || !empleado.Activo)
            return null;

        if (!await users.CheckPasswordAsync(user, password))
        {
            await users.AccessFailedAsync(user);
            return null;
        }

        await users.ResetAccessFailedCountAsync(user);
        return await ObtenerAsync(user.Id, ct);
    }

    public async Task<Usuario?> ObtenerAsync(string id, CancellationToken ct = default)
    {
        var user = await users.FindByIdAsync(id);
        if (user == null || user.SecurityStamp == null || await users.IsLockedOutAsync(user))
            return null;

        var empleado = await empleados.ObtenerPorUsuarioAsync(id, ct);
        if (empleado == null || !empleado.Activo)
            return null;

        var roles = await users.GetRolesAsync(user);
        if (roles.Count == 0 || roles.Any(r => !RolesImportNet.Todos.Contains(r)))
            return null;

        return new Usuario(user.Id, user.Email!, user.SecurityStamp, empleado, [.. roles]);
    }
}
