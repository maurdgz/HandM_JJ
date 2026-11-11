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
        services.AddAuthentication(o => {
            o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            o.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<JwtSettings>((options, settings) => {
                options.MapInboundClaims = false;
                options.IncludeErrorDetails = false;
                options.TokenValidationParameters = new TokenValidationParameters {
                    ValidateIssuer = true, ValidIssuer = settings.Issuer,
                    ValidateAudience = true, ValidAudience = settings.Audience,
                    ValidateIssuerSigningKey = true, IssuerSigningKey = settings.Key,
                    ValidateLifetime = true, RequireExpirationTime = true,
                    ClockSkew = TimeSpan.Zero, NameClaimType = "sub", RoleClaimType = "role",
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
                };
                options.Events = new JwtBearerEvents {
                    OnTokenValidated = async context => {
                        var repository = context.HttpContext.RequestServices.GetRequiredService<IUsuarioRepository>();
                        var principal = context.Principal!;
                        var id = principal.FindFirst("sub")?.Value;
                        var user = id is null ? null : await repository.Obtener(id, context.HttpContext.RequestAborted);
                        var roles = principal.FindAll("role").Select(c => c.Value).Order().ToArray();
                        if (user is null || principal.FindFirst("access_version")?.Value != JwtService.Version(user.VersionAcceso)
                            || principal.FindFirst("empleado_id")?.Value != user.Empleado.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)
                            || !roles.SequenceEqual(user.Roles.Select(r => r.Rol.Nombre).Order()))
                            context.Fail("Acceso revocado.");
                    }
                };
            });
        return services;
    }
}
