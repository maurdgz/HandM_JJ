using ImportNet.Application.Security;
using ImportNet.Application.UseCases.Empleados;
using ImportNet.Application.UseCases.Reportes;
using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure.Identity;
using ImportNet.Infrastructure.Persistence;
using ImportNet.Infrastructure.Reportes;
using ImportNet.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ImportNet.Infrastructure;

public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var connection = config.GetConnectionString("ImportNet") ?? "Host=localhost;Database=ImportNet;Username=postgres";
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connection));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IEmpleadoRepository, EmpleadoRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IUsuarioActual, UsuarioActual>();
        services.AddScoped<IAdministracionEmpleados, AdministracionEmpleados>();
        services.AddSingleton<IExportadorExcel, ExportadorExcel>();
        services.AddSingleton<ImportNet.Application.Security.IPasswordHasher, BcryptPasswordHasher>();
        services.AddHttpContextAccessor();

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
