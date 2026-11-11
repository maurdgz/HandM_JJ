using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ImportNet.Infrastructure.Identity;

internal sealed class UsuarioActual(IHttpContextAccessor context, AppDbContext db) : IUsuarioActual
{
    public string? Id => context.HttpContext?.User.Identity?.IsAuthenticated == true
        ? context.HttpContext.User.FindFirst("sub")?.Value : null;
    public async Task ExigirAdministrador(CancellationToken ct)
    {
        var principal = context.HttpContext?.User;
        var id = Id;
        if (principal?.Identity?.IsAuthenticated != true || id is null) throw new AccesoNoPermitidoException(401);
        var permitido = await (from e in db.Set<Empleado>()
            join ur in db.UserRoles on e.UsuarioId equals ur.UserId
            join role in db.Roles on ur.RoleId equals role.Id
            where e.UsuarioId == id && e.Activo && role.Name == RolesImportNet.Administrador
            select e.Id).AnyAsync(ct);
        if (!permitido) throw new AccesoNoPermitidoException(403);
    }
}
