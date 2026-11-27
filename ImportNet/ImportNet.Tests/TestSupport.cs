using System.Security.Cryptography;
using ImportNet.Application;
using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure;
using ImportNet.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ImportNet.Tests;

internal static class TestSupport
{
    public static void ConfigureJwt(IConfigurationBuilder builder)
        => builder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "ImportNet.Tests",
            ["Jwt:Audience"] = "ImportNet.Tests",
            ["Jwt:SigningKey"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
        });

    public static void UseTestDatabase(IServiceCollection services, SqliteConnection connection)
    {
        services.RemoveAll<DbContextOptions<AppDbContext>>();
        services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(connection));
    }

    public static void CreateTestSchema(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();
        db.Database.ExecuteSqlRaw(db.Database.GenerateCreateScript());
    }
}

internal sealed class ApplicationFixture : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;

    public IMediator Mediator => _scope.ServiceProvider.GetRequiredService<IMediator>();
    public AppDbContext Db => _scope.ServiceProvider.GetRequiredService<AppDbContext>();
    public IUnitOfWork Uow => _scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

    public ApplicationFixture()
    {
        _connection.Open();
        var config = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(config);
        services.AddApplication().AddInfrastructure(config);
        TestSupport.UseTestDatabase(services, _connection);
        _provider = services.BuildServiceProvider();
        _scope = _provider.CreateScope();
        TestSupport.CreateTestSchema(_scope.ServiceProvider);
    }

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
        _connection.Dispose();
    }
}

internal sealed class ApiTestFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public ApiTestFactory()
    {
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) => TestSupport.ConfigureJwt(config));
        builder.UseContentRoot(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../ImportNet.API")));
        builder.ConfigureServices(s => TestSupport.UseTestDatabase(s, _connection));
    }

    public void InitializeSchema()
    {
        using var scope = Services.CreateScope();
        TestSupport.CreateTestSchema(scope.ServiceProvider);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}
