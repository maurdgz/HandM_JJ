using ImportNet.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ImportNet.Tests;

public sealed class ConnectionStringResolverTests
{
    [Fact]
    public void Normalizar_RenderPostgresUri_ConvierteANpgsqlConSslRequire()
    {
        var uri = "postgres://importnet:secret%40123@dpg-12345.oregon-postgres.render.com:5432/importnet_db";
        var cs = ConnectionStringResolver.Normalizar(uri);

        Assert.Contains("Host=dpg-12345.oregon-postgres.render.com", cs);
        Assert.Contains("Database=importnet_db", cs);
        Assert.Contains("Username=importnet", cs);
        Assert.Contains("Password=secret@123", cs);
        Assert.Contains("SSL Mode=Require", cs);
    }

    [Fact]
    public void Resolver_UsaDatabaseUrlSiNoHayConnectionString()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DATABASE_URL"] = "postgresql://user:pass@localhost:5432/localdb"
            })
            .Build();

        var cs = ConnectionStringResolver.Resolver(config);

        Assert.Contains("Host=localhost", cs);
        Assert.Contains("Database=localdb", cs);
        Assert.Contains("SSL Mode=Prefer", cs);
    }
}
