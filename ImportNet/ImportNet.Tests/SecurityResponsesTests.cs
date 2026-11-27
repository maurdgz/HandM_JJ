using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace ImportNet.Tests;

public sealed class SecurityResponsesTests
{
    [Fact]
    public async Task Api_UniformaErroresYNoPermiteElevarRolConCabeceras()
    {
        using var factory = new SmokeFactory();
        using var client = factory.CreateClient();
        factory.InitializeSchema();
        using var anonymous = await client.GetAsync("/api/empleados");
        await Problem(anonymous, HttpStatusCode.Unauthorized);
        Assert.Contains(anonymous.Headers.WwwAuthenticate, h => h.Scheme == "Bearer");
        await SmokeTests.Login(factory, client, "Vendedor");
        client.DefaultRequestHeaders.Add("X-Role", "Administrador");
        foreach (var route in new[] { "/api/empleados", "/api/proveedores", "/api/compras", "/api/inventario/kardex/1" })
            await Problem(await client.GetAsync(route), HttpStatusCode.Forbidden);
        await Problem(await client.GetAsync("/api/no-existe"), HttpStatusCode.NotFound);
        using var method = new HttpRequestMessage(HttpMethod.Trace, "/api/auth/me");
        await Problem(await client.SendAsync(method), HttpStatusCode.MethodNotAllowed);
        var user = await client.GetStringAsync("/api/auth/me");
        foreach (var field in new[] { "password", "hash", "securitystamp", "access_version" })
            Assert.DoesNotContain(field, user, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Comandos_RechazanEntradasNulasIncompletasYJsonInvalidoCon400()
    {
        using var factory = new SmokeFactory();
        using var client = factory.CreateClient();
        factory.InitializeSchema();
        await SmokeTests.Login(factory, client, "Administrador");
        foreach (var (route, json) in new[] {
            ("/api/categorias", "{\"nombre\":\" \"}"),
            ("/api/productos", "{}"),
            ("/api/clientes", "{\"tipoDocumento\":999,\"numeroDocumento\":\"abc\",\"nombre\":\"Prueba\"}"),
            ("/api/proveedores", "{\"nombre\":\"Prueba\",\"tipo\":0,\"pais\":\"Peru\",\"contacto\":\"Prueba\",\"activo\":true,\"productos\":null}"),
            ("/api/empleados", "{\"datos\":null,\"password\":\"Invalid!123456\",\"activo\":true,\"roles\":null}"),
            ("/api/compras", "{\"proveedorId\":1,\"lineas\":null}"),
            ("/api/ventas", "{\"clienteId\":1,\"lineas\":null}"),
            ("/api/ventas", "{\"clienteId\":1,\"lineas\":[null]}"),
            ("/api/ventas/1/anulacion", "{\"motivo\":\" \"}"),
            ("/api/inventario/ajustes", "{}"),
            ("/api/categorias", "{invalido"),
            ("/api/ventas", "null")
        }) {
            using var response = await client.PostAsync(route, new StringContent(json, Encoding.UTF8, "application/json"));
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"{route}: se esperaba 400 y se obtuvo {(int)response.StatusCode}");
            await Problem(response, HttpStatusCode.BadRequest);
        }
    }

    private static async Task Problem(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        Assert.True(response.Headers.CacheControl!.NoStore);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal((int)status, json.RootElement.GetProperty("status").GetInt32());
        Assert.True(json.RootElement.TryGetProperty("traceId", out _));
        var text = json.RootElement.ToString();
        Assert.DoesNotContain("StackTrace", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PasswordHash", text, StringComparison.OrdinalIgnoreCase);
    }
}
