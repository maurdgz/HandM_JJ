using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ImportNet.Infrastructure.Identity;

internal sealed class EmpleadoSignInManager(UserManager<ApplicationUser> users, IHttpContextAccessor context,
    IUserClaimsPrincipalFactory<ApplicationUser> claims, IOptions<IdentityOptions> options,
    ILogger<SignInManager<ApplicationUser>> logger, IAuthenticationSchemeProvider schemes,
    IUserConfirmation<ApplicationUser> confirmation, IEmpleadoRepository empleados)
    : SignInManager<ApplicationUser>(users, context, claims, options, logger, schemes, confirmation)
{
    public override async Task<bool> CanSignInAsync(ApplicationUser user)
        => await base.CanSignInAsync(user) && (await empleados.ObtenerPorUsuario(user.Id, CancellationToken.None))?.Activo == true;
}
