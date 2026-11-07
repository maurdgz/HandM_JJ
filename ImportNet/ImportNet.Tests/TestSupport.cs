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
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace ImportNet.Tests;

internal static class TestSupport
{
    public static HttpClient CreateKestrelClient(WebApplicationFactory<Program> factory)
    {
        var server = factory.Services.GetRequiredService<IServer>();
        var address = server.Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        return new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { BaseAddress = new Uri(address) };
    }
    public static void UseTestDatabase(IServiceCollection services, SqliteConnection connection)
    {
        services.RemoveAll<DbContextOptions<AppDbContext>>();
        services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(connection));
    }
    public static void CreateTestSchema(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();
        // Isolated, transient SQLite only. Never executes against the local PostgreSQL database.
        db.Database.ExecuteSqlRaw(db.Database.GenerateCreateScript());
    }
}

internal sealed class ApplicationFixture : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly ServiceProvider provider;
    private readonly IServiceScope scope;
    public IMediator Mediator => scope.ServiceProvider.GetRequiredService<IMediator>();
    public AppDbContext Db => scope.ServiceProvider.GetRequiredService<AppDbContext>();
    public IUnitOfWork Uow => scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
    public ApplicationFixture()
    {
        connection.Open();
        var config = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(config);
        services.AddApplication().AddInfrastructure(config);
        TestSupport.UseTestDatabase(services, connection);
        provider = services.BuildServiceProvider();
        scope = provider.CreateScope();
        TestSupport.CreateTestSchema(scope.ServiceProvider);
    }
    public void Dispose() { scope.Dispose(); provider.Dispose(); connection.Dispose(); }
}

internal sealed class SmokeFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    public SmokeFactory(bool useKestrel = false)
    {
        connection.Open();
        if (useKestrel) UseKestrel(0);
    }
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseContentRoot(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../ImportNet.API")));
        builder.ConfigureServices(s => TestSupport.UseTestDatabase(s, connection));
    }
    public void InitializeSchema()
    {
        using var scope = Services.CreateScope();
        TestSupport.CreateTestSchema(scope.ServiceProvider);
    }
    protected override void Dispose(bool disposing) { base.Dispose(disposing); if (disposing) connection.Dispose(); }
}
