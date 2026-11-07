using System.Net;
using System.Net.Http.Json;
using ImportNet.Application.Security;
using ImportNet.Application.UseCases.Empleados;
using ImportNet.Application.UseCases.Empleados.Commands;
using ImportNet.Application.UseCases.Empleados.Queries;
using ImportNet.Domain.Entities;
using ImportNet.Infrastructure.Identity;
using ImportNet.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ImportNet.Tests;

public sealed class EmpleadosTests
{
    [Theory]
    [InlineData("Vendedor")]
    [InlineData("Almacenero")]
    public async Task Administracion_ExigeAdministradorEnHttpYCasoDeUso(string role)
    {
        using var factory = new SmokeFactory();
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        factory.InitializeSchema();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/empleados")).StatusCode);
        await SmokeTests.Login(factory, client, role);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/empleados")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/empleados", Nuevo())).StatusCode);
        var mvc = await client.GetAsync("/empleados");
        Assert.Equal(HttpStatusCode.Redirect, mvc.StatusCode);
        Assert.Contains("/cuenta/denegado", mvc.Headers.Location!.ToString());
        using var scope = factory.Services.CreateScope();
        var denied = await Assert.ThrowsAsync<AccesoNoPermitidoException>(() => scope.ServiceProvider.GetRequiredService<IMediator>().Send(new GetAllEmpleadosQuery()));
        Assert.Equal(401, denied.Status);
    }

    [Fact]
    public async Task Inactivo_NoIniciaSesion_YDesactivacionRevocaCookieAlValidarStamp()
    {
        using var original = new SmokeFactory();
        using var factory = original.WithWebHostBuilder(b => b.ConfigureServices(s => s.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.Zero)));
        using var admin = factory.CreateClient(new() { AllowAutoRedirect = false });
        original.InitializeSchema();
        await SmokeTests.Login(factory, admin, "Administrador");
        await Csrf(admin);
        var command = Nuevo() with { Activo = false };
        var response = await admin.PostAsJsonAsync("/api/empleados", command);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = (await response.Content.ReadFromJsonAsync<IdResponse>())!.Id;
        using var empleado = factory.CreateClient(new() { AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.OK, (await SmokeTests.IniciarSesion(empleado, command.Datos.Email, command.Password)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await empleado.GetAsync("/api/productos")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PatchAsJsonAsync($"/api/empleados/{id}/estado", new { activo = true })).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await SmokeTests.IniciarSesion(empleado, command.Datos.Email, command.Password)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await empleado.GetAsync("/api/productos")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PatchAsJsonAsync($"/api/empleados/{id}/estado", new { activo = false })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await empleado.GetAsync("/api/productos")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await SmokeTests.IniciarSesion(empleado, command.Datos.Email, command.Password)).StatusCode);
    }

    [Fact]
    public async Task UltimoAdministrador_NoSeDesactivaNiPierdeRol()
    {
        using var factory = new SmokeFactory();
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        factory.InitializeSchema();
        await SmokeTests.Login(factory, client, "Administrador"); await Csrf(client);
        var root = (await client.GetFromJsonAsync<List<EmpleadoDto>>("/api/empleados"))!.Single();
        Assert.Equal(HttpStatusCode.Conflict, (await client.PatchAsJsonAsync($"/api/empleados/{root.Id}/estado", new { activo = false })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/empleados/{root.Id}/roles", new { roles = new[] { "Vendedor" } })).StatusCode);
        var actual = await client.GetFromJsonAsync<EmpleadoDto>($"/api/empleados/{root.Id}");
        Assert.True(actual!.Activo); Assert.Contains("Administrador", actual.Roles);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/empleados", Nuevo() with { Roles = ["Administrador"] })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PatchAsJsonAsync($"/api/empleados/{root.Id}/estado", new { activo = false })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/empleados")).StatusCode);
    }

    [Fact]
    public async Task RegistroYEdicion_ValidanIdentificadores_CuentaYFormularios()
    {
        using var factory = new SmokeFactory();
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        factory.InitializeSchema();
        await SmokeTests.Login(factory, client, "Administrador"); await Csrf(client);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/empleados/crear")).StatusCode);
        var command = Nuevo();
        var response = await client.PostAsJsonAsync("/api/empleados", command);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = (await response.Content.ReadFromJsonAsync<IdResponse>())!.Id;
        var registered = await client.GetFromJsonAsync<EmpleadoDto>($"/api/empleados/{id}");
        Assert.Equal(command.Datos.DocumentoIdentidad, registered!.DocumentoIdentidad);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/empleados", Nuevo() with { Datos = Nuevo().Datos with { DocumentoIdentidad = command.Datos.DocumentoIdentidad } })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/empleados", Nuevo() with { Datos = Nuevo().Datos with { Email = command.Datos.Email } })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/empleados", Nuevo() with { Roles = ["Superusuario"] })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/empleados", Nuevo() with { Datos = Nuevo().Datos with { Telefono = "-------" } })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/empleados", Nuevo() with { Password = "sololetrasminusculas" })).StatusCode);
        var updated = command.Datos with { Apellidos = "Herrera", Email = $"{Guid.NewGuid():N}@example.test" };
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/empleados/{id}", new UpdateEmpleadoCommand(id, updated with { Nombre = " " }, true, ["Almacenero"]))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync($"/api/empleados/{id}", new UpdateEmpleadoCommand(id, updated, true, ["Almacenero"]))).StatusCode);
        var actual = await client.GetFromJsonAsync<EmpleadoDto>($"/api/empleados/{id}");
        Assert.Equal(registered.UsuarioId, actual!.UsuarioId); Assert.Equal(updated.Email, actual.Email); Assert.Equal(new[] { "Almacenero" }, actual.Roles);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.SingleAsync(u => u.Id == actual.UsuarioId);
            Assert.Equal(updated.Email, user.UserName); Assert.Equal(updated.Telefono, user.PhoneNumber);
            Assert.Equal(2, await db.Set<Empleado>().CountAsync()); Assert.Equal(2, await db.Users.CountAsync());
        }
        var found = await client.GetFromJsonAsync<List<EmpleadoDto>>($"/api/empleados?busqueda={command.Datos.DocumentoIdentidad}&activo=true");
        Assert.Single(found!);
        Assert.DoesNotContain("passwordHash", await client.GetStringAsync($"/api/empleados/{id}"), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/empleados/{id}/editar")).StatusCode);
        var fields = new Dictionary<string,string> { ["Id"] = id.ToString(), ["Nombre"] = updated.Nombre, ["Apellidos"] = updated.Apellidos,
            ["DocumentoIdentidad"] = updated.DocumentoIdentidad, ["Email"] = updated.Email, ["Telefono"] = updated.Telefono,
            ["Cargo"] = "Vendedor", ["Area"] = "Comercial", ["Activo"] = "true", ["Roles"] = "Vendedor" };
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/empleados/guardar", new FormUrlEncodedContent(fields))).StatusCode);
        Assert.Contains("Vendedor", (await client.GetFromJsonAsync<EmpleadoDto>($"/api/empleados/{id}"))!.Roles);
        fields["Nombre"] = "";
        var invalid = await client.PostAsync("/empleados/guardar", new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.OK, invalid.StatusCode);
        Assert.Contains("validation-summary-errors", await invalid.Content.ReadAsStringAsync());
    }
    private static CreateEmpleadoCommand Nuevo()
    {
        var unique = Guid.NewGuid().ToString("N");
        return new(new("Mauricio", "Rodriguez", "EMP" + unique[..12].ToUpperInvariant(), $"{unique}@example.test", "+51987654321", "Vendedor", "Comercial"),
            "Test!aA1" + Guid.NewGuid().ToString("N"), true, ["Vendedor"]);
    }
    private static async Task Csrf(HttpClient client)
    {
        var token = await client.GetFromJsonAsync<CsrfResponse>("/cuenta/csrf");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", token!.Token);
    }
    private sealed record IdResponse(int Id);
    private sealed record CsrfResponse(string Token);
}
