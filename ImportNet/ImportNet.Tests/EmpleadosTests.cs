using System.Net;
using System.Net.Http.Json;
using ImportNet.Application.Security;
using ImportNet.Application.UseCases.Empleados;
using ImportNet.Application.UseCases.Empleados.Commands;
using ImportNet.Application.UseCases.Empleados.Queries;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ImportNet.Tests;

public sealed class EmpleadosTests
{
    [Fact]
    public async Task Administracion_ExigeIdentidadEnCasoDeUso()
    {
        using var factory = new SmokeFactory();
        using var client = factory.CreateClient();
        factory.InitializeSchema();
        using var scope = factory.Services.CreateScope();
        var denied = await Assert.ThrowsAsync<AccesoNoPermitidoException>(() => scope.ServiceProvider.GetRequiredService<IMediator>().Send(new GetAllEmpleadosQuery()));
        Assert.Equal(401, denied.Status);
    }

    [Fact]
    public async Task UltimoAdministrador_NoSeDesactivaNiPierdeRol()
    {
        using var factory = new SmokeFactory();
        using var client = factory.CreateClient();
        factory.InitializeSchema();
        await SmokeTests.Login(factory, client, "Administrador");
        var root = (await client.GetFromJsonAsync<List<EmpleadoDto>>("/api/empleados"))!.Single();
        Assert.Equal(HttpStatusCode.Conflict, (await client.PatchAsJsonAsync($"/api/empleados/{root.Id}/estado", new { activo = false })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/empleados/{root.Id}/roles", new { roles = new[] { "Vendedor" } })).StatusCode);
        var actual = await client.GetFromJsonAsync<EmpleadoDto>($"/api/empleados/{root.Id}");
        Assert.True(actual!.Activo); Assert.Contains("Administrador", actual.Roles);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/empleados", Nuevo() with { Roles = ["Administrador"] })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PatchAsJsonAsync($"/api/empleados/{root.Id}/estado", new { activo = false })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/empleados")).StatusCode);
    }

    [Fact]
    public async Task RegistroYEdicion_ValidanCuentaRolesYUnicidad()
    {
        using var factory = new SmokeFactory();
        using var client = factory.CreateClient();
        factory.InitializeSchema();
        await SmokeTests.Login(factory, client, "Administrador");
        var command = Nuevo();
        var response = await client.PostAsJsonAsync("/api/empleados", command);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = (await response.Content.ReadFromJsonAsync<IdResponse>())!.Id;
        var registered = await client.GetFromJsonAsync<EmpleadoDto>($"/api/empleados/{id}");
        Assert.Equal(command.Datos.DocumentoIdentidad, registered!.DocumentoIdentidad);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/empleados", Nuevo() with { Datos = Nuevo().Datos with { DocumentoIdentidad = command.Datos.DocumentoIdentidad } })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/empleados", Nuevo() with { Datos = Nuevo().Datos with { Email = command.Datos.Email } })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/empleados", Nuevo() with { Roles = ["Superusuario"] })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/empleados", Nuevo() with { Password = "minusculas" })).StatusCode);
        var updated = command.Datos with { Apellidos = "Actualizado", Email = $"{Guid.NewGuid():N}@example.test" };
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync($"/api/empleados/{id}", new UpdateEmpleadoCommand(id, updated, true, ["Administrador"]))).StatusCode);
        var actual = await client.GetFromJsonAsync<EmpleadoDto>($"/api/empleados/{id}");
        Assert.Equal(registered.UsuarioId, actual!.UsuarioId); Assert.Equal(updated.Email, actual.Email);
        Assert.Equal(new[] { "Administrador" }, actual.Roles);
        Assert.Single((await client.GetFromJsonAsync<List<EmpleadoDto>>($"/api/empleados?busqueda={command.Datos.DocumentoIdentidad}&activo=true"))!);
        Assert.DoesNotContain("passwordHash", await client.GetStringAsync($"/api/empleados/{id}"), StringComparison.OrdinalIgnoreCase);
    }

    internal static CreateEmpleadoCommand Nuevo()
    {
        var unique = Guid.NewGuid().ToString("N");
        return new(new("Empleado", "Prueba", "EMP" + unique[..12].ToUpperInvariant(), $"{unique}@example.test", "+51987654321", "Vendedor", "Comercial"),
            "Test!aA1" + Guid.NewGuid().ToString("N"), true, ["Vendedor"]);
    }
    private sealed record IdResponse(int Id);
}
