using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ImportNet.Infrastructure.Identity;

internal sealed class UsuarioActual : IUsuarioActual
{
    private readonly IHttpContextAccessor _httpContext;
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _users;

    public UsuarioActual(IHttpContextAccessor httpContext, AppDbContext context, UserManager<ApplicationUser> users)
    {
        _httpContext = httpContext;
        _context = context;
        _users = users;
    }

    public string? Id
    {
        get
        {
            var user = _httpContext.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return null;

            return user.FindFirst("sub")?.Value;
        }
    }

    public async Task ExigirAdministrador(CancellationToken ct)
    {
        if (Id == null)
            throw new AccesoNoPermitidoException(401);

        if (!await EsAdministrador(ct))
            throw new AccesoNoPermitidoException(403);
    }

    public async Task<bool> EsAdministrador(CancellationToken ct)
    {
        if (Id == null)
            return false;

        var empleado = await _context.Set<Empleado>().AsNoTracking().FirstOrDefaultAsync(e => e.UsuarioId == Id, ct);
        if (empleado == null || !empleado.Activo)
            return false;

        var user = await _users.FindByIdAsync(Id);
        return user != null && await _users.IsInRoleAsync(user, RolesImportNet.Administrador);
    }
}
