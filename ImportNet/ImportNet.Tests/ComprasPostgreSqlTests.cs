using System.Net;
using System.Net.Http.Json;
using ImportNet.Application.UseCases.Compras;
using ImportNet.Domain.Entities;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace ImportNet.Tests;
public sealed class ComprasPostgreSqlTests
{
    [PostgreSqlTheory, Trait("Database", "PostgreSQL"), InlineData("Administrador")]
    public async Task CrudCostosRecibidaYStock_ConRollback(string role)
    {
        using var factory = new PostgreSqlFactory(); using var client = TestSupport.CreateKestrelClient(factory);
        await SmokeTests.Login(factory, client, role); var datos = await ComprasApiTests.Preparar(factory);
        var response = await client.PostAsJsonAsync("/api/compras", datos); Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var c = (await response.Content.ReadFromJsonAsync<CompraDto>())!;
        var edit = await client.PutAsJsonAsync($"/api/compras/{c.Id}", datos with { FletePen = .01m }); Assert.Equal(HttpStatusCode.OK, edit.StatusCode);
        Assert.Equal(81.01m, (await edit.Content.ReadFromJsonAsync<CompraDto>())!.TotalPen);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/compras/{c.Id}")).StatusCode);
        c = (await (await client.PostAsJsonAsync("/api/compras", datos)).Content.ReadFromJsonAsync<CompraDto>())!;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); var entity = await db.Set<Compra>().SingleAsync(x => x.Id == c.Id);
            db.Entry(entity).Property(x => x.Estado).CurrentValue = EstadoCompra.Recibida; await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/compras/{c.Id}", datos)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/compras/{c.Id}")).StatusCode);
        using var check = factory.Services.CreateScope(); var context = check.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(9, (await context.Set<Inventario>().SingleAsync(x => x.ProductoId == datos.Lineas[0].ProductoId)).Cantidad);
        Assert.Equal(0, (await context.Set<Producto>().SingleAsync(x => x.Id == datos.Lineas[0].ProductoId)).CostoPromedio);
        Assert.Empty(await context.Set<MovimientoInventario>().Where(x => x.ProductoId == datos.Lineas[0].ProductoId).ToListAsync());
    }
}
