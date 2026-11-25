using System.Net;
using System.Net.Http.Json;
using ImportNet.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Xunit;

namespace ImportNet.Tests;

public sealed class VentasPostgresFactAttribute : FactAttribute
{
    public VentasPostgresFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("IMPORTNET_VENTAS_POSTGRES_TESTS") != "1")
            Skip = "Requiere PostgreSQL local e IMPORTNET_VENTAS_POSTGRES_TESTS=1. Usa un esquema temporal propio.";
    }
}

internal sealed class VentasPostgresFactory : WebApplicationFactory<Program>
{
    private readonly string schema = "importnet_ventas_prueba_" + Guid.NewGuid().ToString("N");
    private readonly NpgsqlConnection administrator;
    private readonly string connection;
    private bool created;
    private bool disposed;

    public VentasPostgresFactory()
    {
        var config = new ConfigurationBuilder().AddUserSecrets("ImportNet-local-2026").AddEnvironmentVariables().Build();
        var settings = new NpgsqlConnectionStringBuilder(config.GetConnectionString("ImportNet")
            ?? throw new InvalidOperationException("Configure la conexión local de pruebas."));
        if (settings.Host is not ("localhost" or "127.0.0.1" or "::1"))
            throw new InvalidOperationException("Las pruebas de ventas requieren un PostgreSQL local.");
        administrator = new NpgsqlConnection(settings.ConnectionString);
        try
        {
            administrator.Open();
            using var create = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", administrator);
            create.ExecuteNonQuery();
            created = true;
            settings.SearchPath = schema;
            connection = settings.ConnectionString;
        }
        catch
        {
            administrator.Dispose();
            throw;
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) => TestSupport.ConfigureJwt(config));
        builder.UseContentRoot(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../ImportNet.API")));
        builder.ConfigureServices(services => {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connection,
                postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", schema)));
        });
    }

    public async Task Migrar()
    {
        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposed) return;
        disposed = true;
        try { base.Dispose(disposing); }
        finally
        {
            if (disposing)
            {
                try
                {
                    if (created)
                    {
                        using var drop = new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE", administrator);
                        drop.ExecuteNonQuery();
                        created = false;
                    }
                }
                finally { administrator.Dispose(); }
            }
        }
    }
}

public sealed class VentasConcurrenciaPostgreSqlTests
{
    [VentasPostgresFact, Trait("Database", "PostgreSQL")]
    public async Task MigracionesPosYDosAnulacionesSimultaneas_ProducenUnaReposicion()
    {
        using var factory = new VentasPostgresFactory();
        await factory.Migrar();
        using var admin = factory.CreateClient();
        using var seller = factory.CreateClient();
        await SmokeTests.Login(factory, admin, "Administrador");
        await SmokeTests.Login(factory, seller, "Vendedor");
        var productos = await VentasDatosPrueba.Catalogo(factory.Services);
        var venta = await VentasDatosPrueba.Registrar(seller, productos);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var responses = await Task.WhenAll(
            admin.PostAsJsonAsync($"/api/ventas/{venta.Id}/anulacion", new { motivo = "Devolución A" }, timeout.Token),
            admin.PostAsJsonAsync($"/api/ventas/{venta.Id}/anulacion", new { motivo = "Devolución B" }, timeout.Token));
        try
        {
            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.NoContent);
            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
            await VentasDatosPrueba.ComprobarReposicion(factory.Services, venta.Id, productos);
        }
        finally { foreach (var response in responses) response.Dispose(); }
    }
}
