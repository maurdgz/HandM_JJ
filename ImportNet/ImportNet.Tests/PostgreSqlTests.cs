using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ImportNet.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Xunit;
using ImportNet.Application.UseCases.Empleados;
using ImportNet.Application.UseCases.Empleados.Commands;

namespace ImportNet.Tests;

public sealed class PostgreSqlTheoryAttribute : TheoryAttribute
{
    public PostgreSqlTheoryAttribute()
    {
        if (Environment.GetEnvironmentVariable("IMPORTNET_POSTGRES_TESTS") != "1")
            Skip = "Requiere PostgreSQL migrado, User Secrets e IMPORTNET_POSTGRES_TESTS=1. No es una prueba SQLite.";
    }
}

internal sealed class PostgreSqlFactory : WebApplicationFactory<Program>
{
    private readonly NpgsqlConnection connection;
    private readonly NpgsqlTransaction transaction;
    private bool disposed;

    public PostgreSqlFactory()
    {
        var config = new ConfigurationBuilder().AddUserSecrets("ImportNet-local-2026").AddEnvironmentVariables().Build();
        var settings = new NpgsqlConnectionStringBuilder(config.GetConnectionString("ImportNet")
            ?? throw new InvalidOperationException("Configure la conexión en User Secrets antes de ejecutar pruebas PostgreSQL."));
        if (settings.Database != "ImportNet" || settings.Username != "postgres")
            throw new InvalidOperationException("Las pruebas deben apuntar a ImportNet con el usuario postgres.");
        connection = new NpgsqlConnection(settings.ConnectionString);
        connection.Open();
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection).Options);
        if (db.Database.GetPendingMigrations().Any())
            throw new InvalidOperationException("Inspeccione el esquema y aplique las migraciones revisadas antes de probar.");
        transaction = connection.BeginTransaction();
        UseKestrel(0);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseContentRoot(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../ImportNet.API")));
        builder.ConfigureServices(services => {
            services.RemoveAll<AppDbContext>();
            services.AddScoped(_ => {
                var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection).Options);
                db.Database.UseTransaction(transaction);
                return db;
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposed) return;
        disposed = true;
        try { base.Dispose(disposing); }
        finally
        {
            if (disposing) { transaction.Rollback(); transaction.Dispose(); connection.Dispose(); }
        }
    }
}

public sealed class PostgreSqlTests
{
    [PostgreSqlTheory, Trait("Database", "PostgreSQL")]
    [InlineData("Administrador", true)]
    [InlineData("Vendedor", false)]
    public async Task IdentityYCatalogo_ContraPostgreSql_ConRollback(string role, bool puedeEditar)
    {
        using var factory = new PostgreSqlFactory();
        using var client = TestSupport.CreateKestrelClient(factory);
        string testEmail = "", testPassword = "";
        await SmokeTests.Login(factory, client, role, (email, password) => { testEmail = email; testPassword = password; });
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/swagger/index.html")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/swagger/v1/swagger.json")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/categorias")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/productos")).StatusCode);
        var csrf = await client.GetFromJsonAsync<CsrfResponse>("/cuenta/csrf");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf!.Token);
        if (role == "Administrador")
        {
            var profiles = (await client.GetFromJsonAsync<List<EmpleadoDto>>("/api/empleados"))!;
            if (profiles.Count(e => e.Activo && e.Roles.Contains("Administrador")) == 1)
            {
                var current = profiles.Single(e => e.Email == testEmail);
                Assert.Equal(HttpStatusCode.Conflict, (await client.PatchAsJsonAsync($"/api/empleados/{current.Id}/estado", new { activo = false })).StatusCode);
                Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/empleados/{current.Id}/roles", new { roles = new[] { "Vendedor" } })).StatusCode);
            }
            var token = Guid.NewGuid().ToString("N");
            var command = new CreateEmpleadoCommand(new EmpleadoDatos("Empleado", "PostgreSQL", "PG" + token[..12].ToUpperInvariant(),
                $"{token}@example.test", "+51987654321", "Vendedor", "Comercial"), "Test!aA1" + Guid.NewGuid().ToString("N"), true, ["Vendedor"]);
            var registered = await client.PostAsJsonAsync("/api/empleados", command);
            Assert.Equal(HttpStatusCode.Created, registered.StatusCode);
            var empleadoId = (await registered.Content.ReadFromJsonAsync<IdResponse>())!.Id;
            Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync($"/api/empleados/{empleadoId}", new UpdateEmpleadoCommand(empleadoId,
                command.Datos with { Cargo = "Supervisor" }, true, ["Administrador"]))).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await client.PatchAsJsonAsync($"/api/empleados/{empleadoId}/estado", new { activo = false })).StatusCode);
            var saved = await client.GetFromJsonAsync<EmpleadoDto>($"/api/empleados/{empleadoId}");
            Assert.False(saved!.Activo); Assert.Contains("Administrador", saved.Roles);
        }
        else Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/empleados")).StatusCode);
        var unique = Guid.NewGuid().ToString("N");
        var categoria = new { nombre = $"Verificacion-{unique}" };
        var response = await client.PostAsJsonAsync("/api/categorias", categoria);
        if (!puedeEditar)
        {
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/productos", new { })).StatusCode);
        }
        else
        {
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var categoriaId = (await response.Content.ReadFromJsonAsync<IdResponse>())!.Id;
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/categorias/{categoriaId}")).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/categorias", categoria)).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync($"/api/categorias/{categoriaId}", new { nombre = $"Editada-{unique}" })).StatusCode);
            var producto = new { codigo = $"T-{unique}", nombre = "Producto verificación", unidadMedida = "Unidad", peso = 0.125m, precioUsd = 12.34m, partidaArancelaria = "854442", categoriaId, stockMinimo = 2m };
            var created = await client.PostAsJsonAsync("/api/productos", producto);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            var productoId = (await created.Content.ReadFromJsonAsync<IdResponse>())!.Id;
            using var stored = JsonDocument.Parse(await client.GetStringAsync($"/api/productos/{productoId}"));
            Assert.Equal(12.34m, stored.RootElement.GetProperty("precioUsd").GetDecimal());
            Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync($"/api/productos/{productoId}", producto with { precioUsd = 15.75m })).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/categorias/{categoriaId}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/categorias")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/productos")).StatusCode);
            client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
            Assert.Equal(HttpStatusCode.BadRequest, (await client.DeleteAsync($"/api/productos/{productoId}")).StatusCode);
            client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf.Token);
            Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/productos/{productoId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/productos/{productoId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/categorias/{categoriaId}")).StatusCode);
        }
        if (role == "Administrador" && Environment.GetEnvironmentVariable("IMPORTNET_BROWSER_TESTS") == "1")
            await BrowserVerification.Ejecutar(client.BaseAddress!, testEmail, testPassword);
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/cuenta/logout", new FormUrlEncodedContent([]))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/productos")).StatusCode);
        // Disposal rolls back all test users, roles and catalog data. Existing rows are never modified.
    }
    private sealed record CsrfResponse(string Token);
    private sealed record IdResponse(int Id);
}
