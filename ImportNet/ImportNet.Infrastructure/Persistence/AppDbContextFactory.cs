using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
namespace ImportNet.Infrastructure.Persistence;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Model generation requires no database connection or password.
        var config = new ConfigurationBuilder()
            .AddUserSecrets("ImportNet-local-2026")
            .AddEnvironmentVariables().Build();
        var connection = config.GetConnectionString("ImportNet")
            ?? "Host=localhost;Database=ImportNet;Username=postgres";
        return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection).Options);
    }
}
