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
        if (connection.StartsWith("postgres://") || connection.StartsWith("postgresql://"))
            connection = DesdeUrl(connection);
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

    // Convierte una URL de Neon/Render (postgresql://user:pass@host/db?sslmode=require) al formato de Npgsql.
    private static string DesdeUrl(string url)
    {
        var uri = new Uri(url);
        var credenciales = uri.UserInfo.Split(':', 2);
        var builder = new Npgsql.NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = uri.AbsolutePath.Trim('/'),
            Username = Uri.UnescapeDataString(credenciales[0]),
            Password = credenciales.Length > 1 ? Uri.UnescapeDataString(credenciales[1]) : null,
            SslMode = Npgsql.SslMode.Require
        };
        return builder.ConnectionString;
    }
}
