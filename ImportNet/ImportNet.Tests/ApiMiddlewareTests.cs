using System.Net;
using System.Text.Json;
using Xunit;

namespace ImportNet.Tests;

public sealed class ApiMiddlewareTests
{
    [Fact]
    public async Task Health_Devuelve200YHeadersDeSeguridadYCorrelacion()
    {
        using var factory = new ApiTestFactory();
        factory.InitializeSchema();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Correlation-Id"));
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
    }

    [Fact]
    public async Task ApiNoAutorizada_Devuelve401ConProblemDetailsYNoStore()
    {
        using var factory = new ApiTestFactory();
        factory.InitializeSchema();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/productos");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString() ?? "");

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.Equal(401, doc.RootElement.GetProperty("status").GetInt32());
        Assert.True(doc.RootElement.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task ApiInexistente_Devuelve404ConProblemDetailsYPropagaCorrelationId()
    {
        using var factory = new ApiTestFactory();
        factory.InitializeSchema();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Correlation-Id", "req-senior-001");

        using var response = await client.GetAsync("/api/no-existe");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("req-senior-001", response.Headers.GetValues("X-Correlation-Id").Single());

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.Equal(404, doc.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("req-senior-001", doc.RootElement.GetProperty("traceId").GetString());
    }
}
