using ImportNet.Application.Interfaces;
using ImportNet.Application.Security;
using ImportNet.Application.UseCases.Empleados;
using ImportNet.Application.UseCases.Reportes;
using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure.ExternalServices;
using ImportNet.Infrastructure.Identity;
using ImportNet.Infrastructure.Persistence;
using ImportNet.Infrastructure.Reportes;
using ImportNet.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ImportNet.Infrastructure;

public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var connection = ConnectionStringResolver.Resolver(config);
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connection));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IEmpleadoRepository, EmpleadoRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IUsuarioActual, UsuarioActual>();
        services.AddScoped<IAdministracionEmpleados, AdministracionEmpleados>();
        services.AddSingleton<IExportadorExcel, ExportadorExcel>();
        services.AddSingleton<Application.Security.IPasswordHasher, BcryptPasswordHasher>();
        services.AddHttpContextAccessor();

        services.Configure<IaSettings>(config.GetSection(IaSettings.SectionName));
        services.AddHttpClient<IAsistenteIaService, AsistenteIaService>((sp, client) =>
        {
            var settings = sp.GetRequiredService<IOptions<IaSettings>>().Value;
            var timeout = Math.Clamp(settings.TimeoutSeconds <= 0 ? 30 : settings.TimeoutSeconds, 5, 120);
            client.Timeout = TimeSpan.FromSeconds(timeout);
        });

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 12;
                options.User.RequireUniqueEmail = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>();

        services.AddScoped<IPasswordHasher<ApplicationUser>, IdentityPasswordHasher>();
        return services;
    }
}
