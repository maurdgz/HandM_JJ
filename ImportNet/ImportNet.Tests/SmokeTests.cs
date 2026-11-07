using System.Net;
using System.Text.RegularExpressions;
using System.Net.Http.Json;
using ImportNet.Infrastructure.Identity;
using ImportNet.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ImportNet.Tests;

public sealed class SmokeTests
{
    [BrowserFact, Trait("Browser", "Chrome")]
    public async Task Chrome_SwaggerCrudProtegido_LoginYLogout_ConSqlite()
    {
        using var factory = new SmokeFactory(useKestrel: true);
        using var client = TestSupport.CreateKestrelClient(factory);
        factory.InitializeSchema();
        string email = "", password = "";
        await Login(factory, client, "Administrador", (e, p) => { email = e; password = p; });
        await BrowserVerification.Ejecutar(client.BaseAddress!, email, password);
    }
    [Fact]
    public async Task KestrelReal_SirveSwaggerYLoginSinConfundirRutasMvcYApi()
    {
        using var factory = new SmokeFactory(useKestrel: true);
        using var client = TestSupport.CreateKestrelClient(factory);
        Assert.True(client.BaseAddress!.Port > 0);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/swagger/index.html")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/swagger/v1/swagger.json")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/cuenta/login")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/categorias")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/cuenta/csrf")).StatusCode);
    }
    [Fact]
    public async Task Arranque_LoginSwaggerYProteccionAnonima()
    {
        using var factory = new SmokeFactory();
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/cuenta/login")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/swagger/v1/swagger.json")).StatusCode);
        var swagger = await client.GetStringAsync("/swagger/index.html");
        Assert.Contains("/js/swagger-auth.js", swagger);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/js/swagger-auth.js")).StatusCode);
        using var document = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        var paths = document.RootElement.GetProperty("paths");
        Assert.True(paths.EnumerateObject().Count() >= 4);
        foreach (var module in new[] { "categorias", "productos" })
        {
            Assert.True(paths.GetProperty($"/api/{module}").TryGetProperty("get", out _));
            Assert.True(paths.GetProperty($"/api/{module}").TryGetProperty("post", out _));
            Assert.True(paths.GetProperty($"/api/{module}/{{id}}").TryGetProperty("put", out _));
            Assert.True(paths.GetProperty($"/api/{module}/{{id}}").TryGetProperty("delete", out _));
        }
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
    internal static async Task Login(WebApplicationFactory<Program> factory, HttpClient client, string role, Action<string, string>? credentials = null)
    {
        var email = $"{Guid.NewGuid():N}@example.test";
        var password = $"Test!aA1{Guid.NewGuid():N}";
        credentials?.Invoke(email, password);
        using (var scope = factory.Services.CreateScope())
        {
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            if (!await roles.RoleExistsAsync(role))
                Assert.True((await roles.CreateAsync(new IdentityRole(role))).Succeeded);
            var user = new ApplicationUser { UserName = email, Email = email };
            Assert.True((await users.CreateAsync(user, password)).Succeeded);
            Assert.True((await users.AddToRoleAsync(user, role)).Succeeded);
            var db = scope.ServiceProvider.GetRequiredService<ImportNet.Infrastructure.Persistence.AppDbContext>();
            db.Add(new Empleado { UsuarioId = user.Id, Nombre = "Usuario de prueba", Cargo = role, Area = "Pruebas" });
            await db.SaveChangesAsync();
        }
        var response = await IniciarSesion(client, email, password);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }
    internal static async Task<HttpResponseMessage> IniciarSesion(HttpClient client, string email, string password)
    {
        var html = await client.GetStringAsync("/cuenta/login");
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success);
        return await client.PostAsync("/cuenta/login", new FormUrlEncodedContent(new Dictionary<string,string> {
            ["Email"] = email, ["Password"] = password, ["__RequestVerificationToken"] = WebUtility.HtmlDecode(match.Groups[1].Value)
        }));
    }
    private sealed record CsrfResponse(string Token);
    private sealed record IdResponse(int Id);
}
