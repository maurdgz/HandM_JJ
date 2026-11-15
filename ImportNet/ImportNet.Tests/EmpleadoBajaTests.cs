using System.Net;
using System.Net.Http.Json;
using ImportNet.Application.UseCases.Empleados;
using Xunit;

namespace ImportNet.Tests;

public sealed class EmpleadoBajaTests
{
    [Fact]
    public async Task BajaLogica_ExigeAdministradorYConservaEmpleado()
    {
        using var factory = new SmokeFactory();
        using var admin = factory.CreateClient();
        using var seller = factory.CreateClient();

        factory.InitializeSchema();

        await SmokeTests.Login(factory, admin, "Administrador");
        await SmokeTests.Login(factory, seller, "Vendedor");

        var created = await admin.PostAsJsonAsync(
            "/api/empleados",
            EmpleadosTests.Nuevo());

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var id = (await created.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await seller.DeleteAsync($"/api/empleados/{id}")).StatusCode);

        Assert.Equal(
            HttpStatusCode.NoContent,
            (await admin.DeleteAsync($"/api/empleados/{id}")).StatusCode);

        var employee = await admin.GetFromJsonAsync<EmpleadoDto>(
            $"/api/empleados/{id}");

        Assert.NotNull(employee);
        Assert.False(employee.Activo);
    }

    private sealed record IdResponse(int Id);
}