using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ImportNet.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ImportNet.Tests;

internal sealed class LimitedLoginFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    public LimitedLoginFactory() => connection.Open();
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) => {
            TestSupport.ConfigureJwt(config);
            config.AddInMemoryCollection(new Dictionary<string, string?> {
                ["LoginRateLimit:PermitLimit"] = "2", ["LoginRateLimit:WindowSeconds"] = "60"
            });
        });
        builder.UseContentRoot(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../ImportNet.API")));
        builder.ConfigureServices(services => TestSupport.UseTestDatabase(services, connection));
    }
    public void InitializeSchema()
    {
        using var scope = Services.CreateScope();
        TestSupport.CreateTestSchema(scope.ServiceProvider);
    }
    protected override void Dispose(bool disposing) { base.Dispose(disposing); if (disposing) connection.Dispose(); }
}

public sealed class LoginRateLimitTests
{
    [Fact]
    public async Task Login_Devuelve429ProblemDetailsYRetryAfterSinConfiarEnCabecerasDelCliente()
    {
        using var factory = new LimitedLoginFactory();
        using var client = factory.CreateClient();
        factory.InitializeSchema();
        for (var attempt = 0; attempt < 2; attempt++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await SmokeTests.IniciarSesion(client, "missing@example.test", "Incorrect!123456")).StatusCode);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login") {
            Content = JsonContent.Create(new { email = "missing@example.test", password = "Incorrect!123456" })
        };
        request.Headers.Add("X-Forwarded-For", "203.0.113.99");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        Assert.NotNull(response.Headers.RetryAfter);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(429, problem.RootElement.GetProperty("status").GetInt32());
        Assert.True(problem.RootElement.TryGetProperty("traceId", out _));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/productos")).StatusCode);
    }

    [Fact]
    public async Task LimiteDeLogin_NoRevocaUnaSesionValida()
    {
        using var factory = new LimitedLoginFactory();
        using var client = factory.CreateClient();
        factory.InitializeSchema();
        await SmokeTests.Login(factory, client, "Administrador");
        Assert.Equal(HttpStatusCode.Unauthorized, (await SmokeTests.IniciarSesion(client, "missing@example.test", "wrong")).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await SmokeTests.IniciarSesion(client, "missing@example.test", "wrong")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me")).StatusCode);
    }
}
