using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ImportNet.Application.Security;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ImportNet.Tests;

public sealed class SmokeTests
{
    [Fact]
    public async Task Kestrel_SwaggerBearerYProteccionAnonima()
    {
        using var factory = new SmokeFactory(useKestrel: true);
        using var client = TestSupport.CreateKestrelClient(factory);
        factory.InitializeSchema();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/swagger/index.html")).StatusCode);
        using var document = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        var bearer = document.RootElement.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        Assert.Equal("bearer", bearer.GetProperty("scheme").GetString());
        var paths = document.RootElement.GetProperty("paths");
        foreach (var module in new[] { "categorias", "productos" })
        {
            Assert.True(paths.GetProperty($"/api/{module}").TryGetProperty("get", out _));
            Assert.True(paths.GetProperty($"/api/{module}").TryGetProperty("post", out _));
            Assert.True(paths.GetProperty($"/api/{module}/{{id}}").TryGetProperty("put", out _));
            Assert.True(paths.GetProperty($"/api/{module}/{{id}}").TryGetProperty("delete", out _));
        }
        Assert.True(paths.GetProperty("/api/auth/login").TryGetProperty("post", out _));
        Assert.True(paths.GetProperty("/api/auth/me").TryGetProperty("get", out _));
        foreach (var path in new[] { "/api/auth/me", "/api/productos", "/api/categorias", "/api/empleados" })
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(path)).StatusCode);
        foreach (var path in new[] { "/", "/cuenta/login", "/cuenta/csrf", "/productos", "/empleados" })
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(path)).StatusCode);
    }

    [Fact]
    public async Task Administrador_CrudCatalogoYErroresSeguros()
    {
        using var factory = new SmokeFactory();
        using var client = factory.CreateClient();
        factory.InitializeSchema();
        await Login(factory, client, "Administrador");
        var created = await client.PostAsJsonAsync("/api/categorias", new { nombre = "Tecnologia" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<IdResponse>())!.Id;
        Assert.NotNull(created.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/categorias/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/categorias", new { nombre = "TECNOLOGIA" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync($"/api/categorias/{id}", new { nombre = "Accesorios" })).StatusCode);
        var producto = new { codigo = "USB-1", nombre = "Cable", unidadMedida = "Unidad", precioVenta = 12.34m, categoriaId = id, stockMinimo = 2m };
        var saved = await client.PostAsJsonAsync("/api/productos", producto);
        Assert.Equal(HttpStatusCode.Created, saved.StatusCode);
        var productoId = (await saved.Content.ReadFromJsonAsync<IdResponse>())!.Id;
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync($"/api/productos/{productoId}", producto with { precioVenta = 15.75m })).StatusCode);
        using var stored = JsonDocument.Parse(await client.GetStringAsync($"/api/productos/{productoId}"));
        Assert.Equal(15.75m, stored.RootElement.GetProperty("precioVenta").GetDecimal());
        Assert.True(stored.RootElement.TryGetProperty("costoPromedio", out _));
        using (var seller = factory.CreateClient())
        {
            await Login(factory, seller, "Vendedor");
            using var visible = JsonDocument.Parse(await seller.GetStringAsync($"/api/productos/{productoId}"));
            Assert.False(visible.RootElement.TryGetProperty("costoPromedio", out _));
            Assert.DoesNotContain("costoPromedio", await seller.GetStringAsync("/api/productos"));
        }
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/categorias/{id}")).StatusCode);
        var invalid = await client.PostAsJsonAsync("/api/categorias", new { nombre = " " });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal("application/problem+json", invalid.Content.Headers.ContentType!.MediaType);
        var error = await invalid.Content.ReadAsStringAsync();
        Assert.DoesNotContain("PasswordHash", error); Assert.DoesNotContain("StackTrace", error);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/productos/{productoId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/productos/{productoId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/categorias/{id}")).StatusCode);
    }

    [Fact]
    public async Task Vendedor_ConsultaCatalogoYNoAdministra()
    {
        using var factory = new SmokeFactory();
        using var client = factory.CreateClient();
        factory.InitializeSchema();
        await Login(factory, client, "Vendedor");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/productos")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/categorias")).StatusCode);
        foreach (var path in new[] { "/api/productos", "/api/categorias", "/api/empleados" })
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(path, new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/empleados")).StatusCode);
    }

    internal static async Task Login(WebApplicationFactory<Program> factory, HttpClient client, string role, Action<string, string>? credentials = null)
    {
        var (email, password, _) = await AuthApiTests.CrearUsuario(factory, role);
        credentials?.Invoke(email, password);
        var response = await IniciarSesion(client, email, password);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var token = (await response.Content.ReadFromJsonAsync<TokenDto>())!;
        client.DefaultRequestHeaders.Authorization = new("Bearer", token.AccessToken);
    }
    internal static Task<HttpResponseMessage> IniciarSesion(HttpClient client, string email, string password)
        => client.PostAsJsonAsync("/api/auth/login", new { email, password });
    private sealed record IdResponse(int Id);
}
