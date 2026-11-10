using ImportNet.Domain.Entities;
using ImportNet.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ImportNet.Application.UseCases.Empleados;
using ImportNet.Application.Security;

namespace ImportNet.Infrastructure.Identity;

public static class AdminBootstrapper
{
    public static async Task Ejecutar(IServiceProvider services, IConfiguration config)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var email = config["Bootstrap:Email"];
        var password = config["Bootstrap:Password"];
        var nombre = config["Bootstrap:Nombre"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(nombre))
            throw new InvalidOperationException("Configure Bootstrap:Email, Bootstrap:Nombre y Bootstrap:Password mediante secretos.");
        var datos = EmpleadoReglas.Validar(new EmpleadoDatos(nombre, config["Bootstrap:Apellidos"] ?? "",
            config["Bootstrap:DocumentoIdentidad"] ?? "", email, config["Bootstrap:Telefono"] ?? "", "Administrador", "Administración"));
        if (await db.Database.GetPendingMigrationsAsync() is var pending && pending.Any())
            throw new InvalidOperationException("Aplique primero las migraciones revisadas.");
        // Existing accounts are never silently promoted or given a new password.
        if (await users.FindByEmailAsync(datos.Email) != null)
            throw new InvalidOperationException("La cuenta ya existe. El bootstrap no modifica cuentas existentes.");
        await using var tx = await db.Database.BeginTransactionAsync();
        if (db.Database.IsNpgsql()) await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(480071126)");
        foreach (var role in RolesImportNet.Todos)
            if (!await roles.RoleExistsAsync(role)) Exigir(await roles.CreateAsync(new IdentityRole(role)));
        var user = new ApplicationUser { UserName = datos.Email, Email = datos.Email, PhoneNumber = datos.Telefono };
        Exigir(await users.CreateAsync(user, password));
        Exigir(await users.AddToRoleAsync(user, "Administrador"));
        db.Set<Empleado>().Add(new Empleado { UsuarioId = user.Id, Nombre = datos.Nombre, Apellidos = datos.Apellidos,
            DocumentoIdentidad = datos.DocumentoIdentidad, Cargo = datos.Cargo, Area = datos.Area });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }
    private static void Exigir(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException("No se pudo crear la cuenta. Revise formato de correo y política de contraseña (12 caracteres, mayúscula, minúscula, número y símbolo).");
    }
}
