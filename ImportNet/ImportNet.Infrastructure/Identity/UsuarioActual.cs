using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ImportNet.Infrastructure.Identity;

internal sealed class UsuarioActual(
    IHttpContextAccessor httpContext,
    AppDbContext context,
    UserManager<ApplicationUser> users) : IUsuarioActual
{
    public string? Id
    {
        get
        {
            var user = httpContext.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return null;

            return user.FindFirst("sub")?.Value;
        }
    }

    public async Task ExigirAdministradorAsync(CancellationToken ct = default)
    {
        if (Id == null)
            throw new AccesoNoPermitidoException(401);

        if (!await EsAdministradorAsync(ct))
            throw new AccesoNoPermitidoException(403);
    }

    public async Task<bool> EsAdministradorAsync(CancellationToken ct = default)
    {
        if (Id == null)
            return false;

        var empleado = await context.Set<Empleado>()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.UsuarioId == Id, ct);

        if (empleado == null || !empleado.Activo)
            return false;

        var user = await users.FindByIdAsync(Id);
        return user != null && await users.IsInRoleAsync(user, RolesImportNet.Administrador);
    }
}
