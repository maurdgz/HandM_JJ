using ImportNet.Application.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace ImportNet.Infrastructure.Identity;

public static class JwtRegistration
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<JwtSettings>();
        services.AddSingleton<IJwtService, JwtService>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<JwtSettings>((options, settings) =>
            {
                options.MapInboundClaims = false;
                options.IncludeErrorDetails = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = settings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = settings.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = settings.Key,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = "sub",
                    RoleClaimType = "role"
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = ValidarCuentaVigente
                };
            });

        return services;
    }

    private static async Task ValidarCuentaVigente(TokenValidatedContext context)
    {
        var usuarios = context.HttpContext.RequestServices.GetRequiredService<IUsuarioRepository>();
        var id = context.Principal!.FindFirst("sub")?.Value;
        var usuario = id == null ? null : await usuarios.Obtener(id, context.HttpContext.RequestAborted);

        if (usuario == null)
        {
            context.Fail("Acceso revocado.");
            return;
        }

        var version = context.Principal.FindFirst("access_version")?.Value;
        var empleadoId = context.Principal.FindFirst("empleado_id")?.Value;
        var rolesToken = context.Principal.FindAll("role").Select(c => c.Value).Order().ToList();
        var rolesActuales = usuario.Roles.Order().ToList();

        if (version != JwtService.Version(usuario.VersionAcceso) || empleadoId != usuario.Empleado.Id.ToString()
            || !rolesToken.SequenceEqual(rolesActuales))
            context.Fail("Acceso revocado.");
    }
}
