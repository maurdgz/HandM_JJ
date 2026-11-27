using ImportNet.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Xunit;

namespace ImportNet.Tests;

public sealed class DashboardPostgreSqlTests
{
    [PostgreSqlTheory, Trait("Database", "PostgreSQL"), InlineData(true)]
    public async Task AgregacionesReales_CoincidenConReportesYHoraPeru(bool _)
    {
        using var factory = new DashboardPostgreSqlFactory();
        using var client = TestSupport.CreateKestrelClient(factory);
        await DashboardTests.VerificarIndicadores(factory, client);
    }
}

/// <summary>Esquema exclusivo de pruebas con conexiones independientes; nunca borra tablas de public.</summary>
internal sealed class DashboardPostgreSqlFactory : WebApplicationFactory<Program>
{
    private readonly string esquema = "dashboard_test_" + Guid.NewGuid().ToString("N");
    private readonly NpgsqlConnection administrador;
    private readonly string conexion;
    private readonly IInterceptor? interceptor;

    public DashboardPostgreSqlFactory(IInterceptor? interceptor = null)
    {
        this.interceptor = interceptor;
        var config = new ConfigurationBuilder().AddUserSecrets("ImportNet-local-2026").AddEnvironmentVariables().Build();
        var settings = new NpgsqlConnectionStringBuilder(config.GetConnectionString("ImportNet"));
        if (settings.Host is not ("localhost" or "127.0.0.1" or "::1") || settings.Database != "ImportNet")
            throw new InvalidOperationException("Las pruebas requieren PostgreSQL local ImportNet.");
        administrador = new NpgsqlConnection(settings.ConnectionString);
        administrador.Open();
        using (var command = administrador.CreateCommand())
        {
            command.CommandText = $"CREATE SCHEMA \"{esquema}\"";
            command.ExecuteNonQuery();
        }
        settings.SearchPath = esquema;
        conexion = settings.ConnectionString;
        try
        {
            using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(conexion, o => o.MigrationsHistoryTable("__EFMigrationsHistory", esquema)).Options);
            db.Database.Migrate();
            UseKestrel(0);
        }
        catch { Dispose(); throw; }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) => TestSupport.ConfigureJwt(config));
        builder.UseContentRoot(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../ImportNet.API")));
        builder.ConfigureServices(services => {
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(o => {
                o.UseNpgsql(conexion, p => p.MigrationsHistoryTable("__EFMigrationsHistory", esquema));
                if (interceptor is not null) o.AddInterceptors(interceptor);
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && administrador.State == System.Data.ConnectionState.Open)
        {
            // esquema deriva únicamente del GUID creado por esta instancia.
            using var command = administrador.CreateCommand();
            command.CommandText = $"DROP SCHEMA IF EXISTS \"{esquema}\" CASCADE";
            command.ExecuteNonQuery();
            administrador.Dispose();
        }
    }
}
