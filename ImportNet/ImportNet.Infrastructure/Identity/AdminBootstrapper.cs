using ImportNet.Application.Security;
using ImportNet.Application.UseCases.Empleados;
using ImportNet.Domain.Entities;
using ImportNet.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ImportNet.Infrastructure.Identity;

public static class AdminBootstrapper
{
    public static async Task EjecutarAsync(IServiceProvider services, IConfiguration config, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        var email = config["Bootstrap:Email"];
        var password = config["Bootstrap:Password"];
        var nombre = config["Bootstrap:Nombre"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(nombre))
            throw new InvalidOperationException("Configure Bootstrap:Email, Bootstrap:Nombre y Bootstrap:Password mediante secretos o variables de entorno.");

        var datos = EmpleadoReglas.Validar(new EmpleadoDatos(
            nombre,
            config["Bootstrap:Apellidos"] ?? "Admin",
            config["Bootstrap:DocumentoIdentidad"] ?? "00000001",
            email,
            config["Bootstrap:Telefono"] ?? "+51 999999999",
            "Administrador",
            "Administración"));

        var pendientes = await db.Database.GetPendingMigrationsAsync(ct);
        if (pendientes.Any())
            throw new InvalidOperationException("Aplique primero las migraciones revisadas.");

        if (await users.FindByEmailAsync(datos.Email) != null)
            throw new InvalidOperationException("La cuenta ya existe. El bootstrap no modifica cuentas existentes.");

        if (await ExisteAdministradorActivoAsync(db, users))
            throw new InvalidOperationException("Ya existe un Administrador activo. Cree cuentas desde la API de empleados.");

        await CrearAdministradorAsync(db, users, roles, datos, password, ct);
    }

    internal static async Task EjecutarSiNoExisteAdministradorAsync(
        IServiceProvider services,
        IConfiguration config,
        ILogger logger,
        CancellationToken ct = default)
    {
        var email = config["Bootstrap:Email"];
        var password = config["Bootstrap:Password"];
        var nombre = config["Bootstrap:Nombre"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(nombre))
        {
            logger.LogWarning("Bootstrap:AutoCreateOnStartup está activo, pero faltan Bootstrap:Email, Bootstrap:Nombre o Bootstrap:Password.");
            return;
        }

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        if (await ExisteAdministradorActivoAsync(db, users) || await users.FindByEmailAsync(email.Trim()) != null)
            return;

        var datos = EmpleadoReglas.Validar(new EmpleadoDatos(
            nombre,
            config["Bootstrap:Apellidos"] ?? "Principal",
            config["Bootstrap:DocumentoIdentidad"] ?? "00000001",
            email,
            config["Bootstrap:Telefono"] ?? "+51 999999999",
            "Administrador",
            "Administración"));

        await CrearAdministradorAsync(db, users, roles, datos, password, ct);
        logger.LogInformation("Administrador inicial creado automáticamente para {Email}.", datos.Email);
    }

    private static async Task<bool> ExisteAdministradorActivoAsync(AppDbContext db, UserManager<ApplicationUser> users)
    {
        var administradores = await users.GetUsersInRoleAsync(RolesImportNet.Administrador);
        var ids = administradores.Select(u => u.Id).ToList();
        return await db.Set<Empleado>().AnyAsync(e => ids.Contains(e.UsuarioId) && e.Activo);
    }

    private static async Task CrearAdministradorAsync(
        AppDbContext db,
        UserManager<ApplicationUser> users,
        RoleManager<IdentityRole> roles,
        EmpleadoDatos datos,
        string password,
        CancellationToken ct)
    {
        await using var transaccion = await db.Database.BeginTransactionAsync(ct);

        foreach (var rol in RolesImportNet.Todos)
        {
            if (!await roles.RoleExistsAsync(rol))
                Exigir(await roles.CreateAsync(new IdentityRole(rol)));
        }

        var user = new ApplicationUser { UserName = datos.Email, Email = datos.Email, PhoneNumber = datos.Telefono };
        Exigir(await users.CreateAsync(user, password));
        Exigir(await users.AddToRoleAsync(user, RolesImportNet.Administrador));

        db.Set<Empleado>().Add(new Empleado
        {
            UsuarioId = user.Id,
            Nombre = datos.Nombre,
            Apellidos = datos.Apellidos,
            DocumentoIdentidad = datos.DocumentoIdentidad,
            Cargo = datos.Cargo,
            Area = datos.Area
        });

        await db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);
    }

    private static void Exigir(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException("No se pudo crear la cuenta. Revise formato de correo y política de contraseña (12 caracteres, mayúscula, minúscula, número y símbolo).");
    }
}
