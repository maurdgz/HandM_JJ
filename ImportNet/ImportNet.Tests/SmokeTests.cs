using System.Net;
using System.Text.RegularExpressions;
using System.Net.Http.Json;
using ImportNet.Infrastructure.Identity;
using ImportNet.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ImportNet.Tests;

public sealed class SmokeTests
{
    [Fact]
    public async Task Arranque_LoginSwaggerYProteccionAnonima()
    {
        using var factory = new SmokeFactory();
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/cuenta/login")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/swagger/v1/swagger.json")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/productos")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/categorias")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/cuenta/login", new FormUrlEncodedContent([]))).StatusCode);
    }
    [Fact]
    public async Task Administrador_LoginCrudMvcApiAntiforgeryYLogout()
    {
        using var factory = new SmokeFactory();
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        factory.InitializeSchema();
        await Login(factory, client, "Administrador");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/categorias/crear")).StatusCode);
        var csrf = await client.GetFromJsonAsync<CsrfResponse>("/cuenta/csrf");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf!.Token);
        var created = await client.PostAsJsonAsync("/api/categorias", new { nombre = "Tecnología" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<IdResponse>())!.Id;
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/categorias/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/categorias", new { nombre = "Tecnología" })).StatusCode);
        var product = new { codigo = "usb-1", nombre = "Cable", descripcion = (string?)null, unidadMedida = "Unidad", peso = 0.1m, precioUsd = 3m, partidaArancelaria = "854442", categoriaId = id, stockMinimo = 2m };
        var productCreated = await client.PostAsJsonAsync("/api/productos", product);
        Assert.Equal(HttpStatusCode.Created, productCreated.StatusCode);
        var productoId = (await productCreated.Content.ReadFromJsonAsync<IdResponse>())!.Id;
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/categorias/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/productos")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/productos/{productoId}/editar")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync($"/api/productos/{productoId}", product with { nombre = "Cable nuevo" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/productos/{productoId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync($"/api/categorias/{id}", new { nombre = "Accesorios" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/categorias/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/categorias/{id}")).StatusCode);
        var mvc = await client.PostAsync("/categorias/guardar", new FormUrlEncodedContent(new Dictionary<string,string> { ["Nombre"] = "Desde MVC" }));
        Assert.Equal(HttpStatusCode.Redirect, mvc.StatusCode);
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/categorias", new { nombre = "Sin token" })).StatusCode);
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf.Token);
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/cuenta/logout", new FormUrlEncodedContent([]))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/categorias")).StatusCode);
    }
    [Fact]
    public async Task Vendedor_ConsultaPeroNoPuedeModificar()
    {
        using var factory = new SmokeFactory();
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        factory.InitializeSchema();
        await Login(factory, client, "Vendedor");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/productos")).StatusCode);
        var csrf = await client.GetFromJsonAsync<CsrfResponse>("/cuenta/csrf");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf!.Token);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/categorias", new { nombre = "Prohibido" })).StatusCode);
    }
    [Fact]
    public async Task Almacenero_FormularioProductoGuardaEditaYPresentaErrores()
    {
        using var factory = new SmokeFactory();
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        factory.InitializeSchema();
        await Login(factory, client, "Almacenero");
        var categoriaForm = await client.GetStringAsync("/categorias/crear");
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/categorias/guardar", Form(new() {
            ["Nombre"] = "Accesorios", ["__RequestVerificationToken"] = Token(categoriaForm)
        }))).StatusCode);
        var categorias = await client.GetFromJsonAsync<List<IdResponse>>("/api/categorias");
        var categoria = categorias!.Single().Id;
        var productForm = await client.GetStringAsync("/productos/crear");
        Assert.Contains("Accesorios", productForm);
        var fields = new Dictionary<string,string> {
            ["Id"] = "0", ["Codigo"] = "mvc-1", ["Nombre"] = "Producto MVC", ["UnidadMedida"] = "Unidad",
            ["Peso"] = "0", ["PrecioUsd"] = "9.50", ["PartidaArancelaria"] = "854442", ["CategoriaId"] = categoria.ToString(),
            ["StockMinimo"] = "2", ["__RequestVerificationToken"] = Token(productForm)
        };
        var invalid = await client.PostAsync("/productos/guardar", Form(fields));
        Assert.Equal(HttpStatusCode.OK, invalid.StatusCode);
        Assert.Contains("validation-summary-errors", await invalid.Content.ReadAsStringAsync());
        Assert.Empty((await client.GetFromJsonAsync<List<IdResponse>>("/api/productos"))!);
        fields["Peso"] = "0.125";
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/productos/guardar", Form(fields))).StatusCode);
        var producto = (await client.GetFromJsonAsync<List<IdResponse>>("/api/productos"))!.Single().Id;
        var editForm = await client.GetStringAsync($"/productos/{producto}/editar");
        fields["Id"] = producto.ToString(); fields["Nombre"] = "Producto editado";
        fields["__RequestVerificationToken"] = Token(editForm);
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/productos/guardar", Form(fields))).StatusCode);
        Assert.Contains("Producto editado", await client.GetStringAsync("/productos"));
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync($"/productos/{producto}/eliminar", Form(new() { ["__RequestVerificationToken"] = Token(editForm) }))).StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<List<IdResponse>>("/api/productos"))!);
    }
    [Fact]
    public async Task Api_ValidaEntradaYDevuelveProblemDetailsSeguro()
    {
        using var factory = new SmokeFactory();
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        factory.InitializeSchema();
        await Login(factory, client, "Administrador");
        var csrf = await client.GetFromJsonAsync<CsrfResponse>("/cuenta/csrf");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf!.Token);
        var invalid = await client.PostAsJsonAsync("/api/categorias", new { nombre = " " });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal("application/problem+json", invalid.Content.Headers.ContentType!.MediaType);
        var error = await invalid.Content.ReadAsStringAsync();
        Assert.DoesNotContain("PasswordHash", error);
        Assert.DoesNotContain("StackTrace", error);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/productos/9999")).StatusCode);
    }
    private static FormUrlEncodedContent Form(Dictionary<string,string> values) => new(values);
    private static string Token(string html)
    {
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success);
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }
    private static async Task Login(SmokeFactory factory, HttpClient client, string role)
    {
        var email = $"{Guid.NewGuid():N}@example.test";
        var password = $"Test!aA1{Guid.NewGuid():N}";
        using (var scope = factory.Services.CreateScope())
        {
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            Assert.True((await roles.CreateAsync(new IdentityRole(role))).Succeeded);
            var user = new ApplicationUser { UserName = email, Email = email };
            Assert.True((await users.CreateAsync(user, password)).Succeeded);
            Assert.True((await users.AddToRoleAsync(user, role)).Succeeded);
        }
        var html = await client.GetStringAsync("/cuenta/login");
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success);
        var response = await client.PostAsync("/cuenta/login", new FormUrlEncodedContent(new Dictionary<string,string> {
            ["Email"] = email, ["Password"] = password, ["__RequestVerificationToken"] = WebUtility.HtmlDecode(match.Groups[1].Value)
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }
    private sealed record CsrfResponse(string Token);
    private sealed record IdResponse(int Id);
}
