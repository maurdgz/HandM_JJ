using System.Net;
using System.Net.Http.Json;
using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Infrastructure.Identity;
using ImportNet.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ImportNet.Tests;

public sealed class AuthApiTests
{
    [Fact]
    public async Task LoginHttp_TransicionIdentityYCredencialesInvalidas()
    {
        using var factory = new SmokeFactory();
        using var client = factory.CreateClient();
        factory.InitializeSchema();
        var (email, password, id) = await CrearUsuario(factory, "Administrador", legacy: true);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new { email, password = "wrong" })).StatusCode);
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var token = await login.Content.ReadFromJsonAsync<TokenDto>();
        Assert.Equal("Bearer", token!.TokenType);
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.StartsWith("{bcrypt-sha384}", (await users.FindByIdAsync(id))!.PasswordHash);
    }

    internal static async Task<(string Email, string Password, string Id)> CrearUsuario(WebApplicationFactory<Program> factory,
        string role, bool legacy = false, bool activo = true)
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roles.RoleExistsAsync(role)) Assert.True((await roles.CreateAsync(new IdentityRole(role))).Succeeded);
        var email = $"{Guid.NewGuid():N}@example.test";
        var password = "Test!Aa123" + Guid.NewGuid().ToString("N");
        var user = new ApplicationUser { UserName = email, Email = email };
        Assert.True((await users.CreateAsync(user, password)).Succeeded);
        if (legacy)
        {
            user.PasswordHash = new PasswordHasher<ApplicationUser>().HashPassword(user, password);
            Assert.True((await users.UpdateAsync(user)).Succeeded);
        }
        Assert.True((await users.AddToRoleAsync(user, role)).Succeeded);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Add(new Empleado { UsuarioId = user.Id, Nombre = "Prueba", Apellidos = "API", Cargo = role, Area = "Pruebas", Activo = activo });
        await db.SaveChangesAsync();
        return (email, password, user.Id);
    }
}
