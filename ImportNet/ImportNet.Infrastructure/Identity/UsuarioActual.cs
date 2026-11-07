using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ImportNet.Infrastructure.Identity;

internal sealed class UsuarioActual(IHttpContextAccessor context, UserManager<ApplicationUser> users, AppDbContext db) : IUsuarioActual
{
    public async Task ExigirAdministrador(CancellationToken ct)
    {
        var principal = context.HttpContext?.User;
        var id = principal is not null ? users.GetUserId(principal) : null;
        if (principal?.Identity?.IsAuthenticated != true || id is null) throw new AccesoNoPermitidoException(401);
        var permitido = await (from e in db.Set<Empleado>()
            join ur in db.UserRoles on e.UsuarioId equals ur.UserId
            join role in db.Roles on ur.RoleId equals role.Id
            where e.UsuarioId == id && e.Activo && role.Name == RolesImportNet.Administrador
            select e.Id).AnyAsync(ct);
        if (!permitido) throw new AccesoNoPermitidoException(403);
    }
}
