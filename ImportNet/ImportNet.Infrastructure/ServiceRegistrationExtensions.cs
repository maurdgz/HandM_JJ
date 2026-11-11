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
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddSingleton<ImportNet.Application.Security.IPasswordHasher, BcryptPasswordHasher>();
        services.AddHttpContextAccessor();
        services.AddScoped<IUsuarioActual, UsuarioActual>();
        services.AddScoped<IAdministracionEmpleados, AdministracionEmpleados>();
        // Retain account storage, validation and transaction semantics, without cookie authentication.
        services.AddIdentityCore<ApplicationUser>(o => {
            o.Password.RequiredLength = 12;
            o.User.RequireUniqueEmail = true;
            o.Lockout.MaxFailedAccessAttempts = 5;
        }).AddRoles<IdentityRole>().AddEntityFrameworkStores<AppDbContext>();
        services.AddScoped<IPasswordHasher<ApplicationUser>, IdentityPasswordTransition>();
        return services;
    }
}
