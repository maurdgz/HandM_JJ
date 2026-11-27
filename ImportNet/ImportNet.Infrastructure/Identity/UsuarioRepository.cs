using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace ImportNet.Infrastructure.Identity;

internal sealed class UsuarioRepository : IUsuarioRepository
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly IEmpleadoRepository _empleados;

    public UsuarioRepository(UserManager<ApplicationUser> users, IEmpleadoRepository empleados)
    {
        _users = users;
        _empleados = empleados;
    }

    public async Task<Usuario?> Autenticar(string email, string password, CancellationToken ct)
    {
        var user = await _users.FindByEmailAsync(email);
        if (user == null || await _users.IsLockedOutAsync(user))
            return null;

        var empleado = await _empleados.ObtenerPorUsuario(user.Id, ct);
        if (empleado == null || !empleado.Activo)
            return null;

        if (!await _users.CheckPasswordAsync(user, password))
        {
            await _users.AccessFailedAsync(user);
            return null;
        }

        await _users.ResetAccessFailedCountAsync(user);
        return await Obtener(user.Id, ct);
    }

    public async Task<Usuario?> Obtener(string id, CancellationToken ct)
    {
        var user = await _users.FindByIdAsync(id);
        if (user == null || user.SecurityStamp == null || await _users.IsLockedOutAsync(user))
            return null;

        var empleado = await _empleados.ObtenerPorUsuario(id, ct);
        if (empleado == null || !empleado.Activo)
            return null;

        var roles = await _users.GetRolesAsync(user);
        if (roles.Count == 0 || roles.Any(r => !RolesImportNet.Todos.Contains(r)))
            return null;

        return new Usuario(user.Id, user.Email!, user.SecurityStamp, empleado, roles.ToList());
    }
}
