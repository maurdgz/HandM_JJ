using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure.Identity;
using ImportNet.Infrastructure.Persistence;
using ImportNet.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ImportNet.Application.Security;
using ImportNet.Application.UseCases.Empleados;
namespace ImportNet.Infrastructure;

public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(o => o.UseNpgsql(config.GetConnectionString("ImportNet")
            ?? "Host=localhost;Database=ImportNet;Username=postgres"));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IEmpleadoRepository, EmpleadoRepository>();
        services.AddHttpContextAccessor();
        services.AddScoped<IUsuarioActual, UsuarioActual>();
        services.AddScoped<IAdministracionEmpleados, AdministracionEmpleados>();
        services.AddIdentity<ApplicationUser, IdentityRole>(o => {
            o.Password.RequiredLength = 12;
            o.User.RequireUniqueEmail = true;
            o.Lockout.MaxFailedAccessAttempts = 5;
        }).AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders().AddSignInManager<EmpleadoSignInManager>();
        services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromMinutes(1));
        services.ConfigureApplicationCookie(o => {
            o.LoginPath = "/cuenta/login";
            o.AccessDeniedPath = "/cuenta/denegado";
            o.Cookie.HttpOnly = true;
            o.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
            o.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
            o.Events.OnRedirectToLogin = c => {
                if (c.Request.Path.StartsWithSegments("/api") || c.Request.Path == "/cuenta/csrf") c.Response.StatusCode = 401;
                else c.Response.Redirect(c.RedirectUri);
                return Task.CompletedTask;
            };
            o.Events.OnRedirectToAccessDenied = c => {
                if (c.Request.Path.StartsWithSegments("/api")) c.Response.StatusCode = 403;
                else c.Response.Redirect(c.RedirectUri);
                return Task.CompletedTask;
            };
        });
        return services;
    }
}
