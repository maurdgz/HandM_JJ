using System.Net;
using System.Net.Http.Json;
using ImportNet.Application.UseCases.Compras;
using ImportNet.Domain.Entities;
using ImportNet.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace ImportNet.Tests;
public sealed class ComprasApiTests
{
    internal static async Task<CompraDatos> Preparar(SmokeFactory factory)
    {
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var categoria = new Categoria("Importacion"); var proveedor = new Proveedor { Nombre = "Proveedor pruebas", Pais = "China", Contacto = "Ventas" };
        db.AddRange(categoria, proveedor); await db.SaveChangesAsync();
        var p = new Producto("IMP-1", "Olla importada", null, "Unidad", null, 30, null, categoria.Id, 1);
        db.Add(p); await db.SaveChangesAsync();
        db.Add(new Inventario { ProductoId = p.Id, AlmacenId = Almacen.PrincipalId, Cantidad = 9 }); await db.SaveChangesAsync();
        return new(proveedor.Id, DateTime.UtcNow, 3.8m, 10, 5, [new(p.Id, 2, 10)]);
    }
    [Fact] public async Task Administrador_CrudConTotalesConfirmadosYSwagger()
    {
        using var factory = new SmokeFactory(); using var client = factory.CreateClient(); factory.InitializeSchema();
        await SmokeTests.Login(factory, client, "Administrador"); var datos = await Preparar(factory);
        var response = await client.PostAsJsonAsync("/api/compras", datos); Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var c = (await response.Content.ReadFromJsonAsync<CompraDto>())!; Assert.Equal(91, c.TotalPen); Assert.Equal("Borrador", c.Estado);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(response.Headers.Location)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/compras")).StatusCode);
        var editada = await client.PutAsJsonAsync($"/api/compras/{c.Id}", datos with { FletePen = 20 }); Assert.Equal(HttpStatusCode.OK, editada.StatusCode);
        Assert.Equal(101, (await editada.Content.ReadFromJsonAsync<CompraDto>())!.TotalPen);
        Assert.Contains("/api/compras", await client.GetStringAsync("/swagger/v1/swagger.json"));
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/compras/{c.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/compras/{c.Id}")).StatusCode);
    }
}
