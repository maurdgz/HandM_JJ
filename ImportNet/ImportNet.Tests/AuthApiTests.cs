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
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ImportNet.Tests;

public sealed class AuthApiTests
{
    [Fact]
    public async Task Bearer_RechazaVencidosManipuladosInactivosYCambiosDeAcceso()
    {
        using var factory = new SmokeFactory();
        using var admin = factory.CreateClient();
        using var seller = factory.CreateClient();
        factory.InitializeSchema();
        await SmokeTests.Login(factory, admin, "Administrador");
        var (email, password, _) = await CrearUsuario(factory, "Vendedor");
        var inactive = await CrearUsuario(factory, "Vendedor", activo: false);
        Assert.Equal(HttpStatusCode.Unauthorized, (await seller.PostAsJsonAsync("/api/auth/login", new { inactive.Email, inactive.Password })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await seller.GetAsync("/api/auth/me")).StatusCode);
        var response = await SmokeTests.IniciarSesion(seller, email, password);
        var result = (await response.Content.ReadFromJsonAsync<TokenDto>())!;
        seller.DefaultRequestHeaders.Authorization = new("Bearer", result.AccessToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await seller.GetAsync("/api/empleados")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await seller.PostAsJsonAsync("/api/empleados", EmpleadosTests.Nuevo())).StatusCode);
        var me = (await seller.GetFromJsonAsync<UsuarioDto>("/api/auth/me"))!;
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);
        var config = factory.Services.GetRequiredService<IConfiguration>();
        var settings = new JwtSettings(config);
        var expired = new JwtSecurityToken(settings.Issuer, settings.Audience, token.Claims,
            DateTime.UtcNow.AddHours(-3), DateTime.UtcNow.AddHours(-1), new SigningCredentials(settings.Key, SecurityAlgorithms.HmacSha256));
        seller.DefaultRequestHeaders.Authorization = new("Bearer", new JwtSecurityTokenHandler().WriteToken(expired));
        Assert.Equal(HttpStatusCode.Unauthorized, (await seller.GetAsync("/api/auth/me")).StatusCode);
        var parts = result.AccessToken.Split('.');
        parts[2] = (parts[2][0] == 'A' ? "B" : "A") + parts[2][1..];
        seller.DefaultRequestHeaders.Authorization = new("Bearer", string.Join('.', parts));
        Assert.Equal(HttpStatusCode.Unauthorized, (await seller.GetAsync("/api/productos")).StatusCode);
        seller.DefaultRequestHeaders.Authorization = new("Bearer", result.AccessToken);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync($"/api/empleados/{me.EmpleadoId}/roles", new { roles = new[] { "Administrador" } })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await seller.GetAsync("/api/auth/me")).StatusCode);
        var renewed = (await (await SmokeTests.IniciarSesion(seller, email, password)).Content.ReadFromJsonAsync<TokenDto>())!;
        seller.DefaultRequestHeaders.Authorization = new("Bearer", renewed.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await seller.GetAsync("/api/empleados")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PatchAsJsonAsync($"/api/empleados/{me.EmpleadoId}/estado", new { activo = false })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await seller.GetAsync("/api/productos")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await SmokeTests.IniciarSesion(seller, email, password)).StatusCode);
    }

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
        client.DefaultRequestHeaders.Authorization = new("Bearer", token.AccessToken);
        var me = await client.GetFromJsonAsync<UsuarioDto>("/api/auth/me");
        Assert.Equal(id, me!.Id);
        Assert.Contains("Administrador", me.Roles);
        var json = await client.GetStringAsync("/api/auth/me");
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stamp", json, StringComparison.OrdinalIgnoreCase);
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
