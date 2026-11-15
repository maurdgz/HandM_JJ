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
    internal static async Task<CompraDatos> Preparar(Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var categoria = new Categoria("Importacion"); var proveedor = new Proveedor { Nombre = "Proveedor pruebas", Pais = "China", Contacto = "Ventas" };
        db.AddRange(categoria, proveedor); await db.SaveChangesAsync();
        var p = new Producto("IMP-1", "Olla importada", null, "Unidad", null, 30, null, categoria.Id, 1);
        db.Add(p); await db.SaveChangesAsync();
        db.Add(new Inventario { ProductoId = p.Id, AlmacenId = Almacen.PrincipalId, Cantidad = 9 }); await db.SaveChangesAsync();
        return new(proveedor.Id, DateTime.UtcNow, 3.8m, 10, 5, [new(p.Id, 2, 10)]);
    }
    [Fact] public async Task Permisos_TodasLasOperaciones401Y403()
    {
        using var factory = new SmokeFactory(); using var anon = factory.CreateClient(); using var seller = factory.CreateClient(); factory.InitializeSchema();
        await SmokeTests.Login(factory, seller, "Vendedor");
        var datos = new CompraDatos(1, DateTime.UtcNow, 3.8m, 0, 0, [new(1, 1, 1)]);
        foreach (var (client, status) in new[] { (anon, HttpStatusCode.Unauthorized), (seller, HttpStatusCode.Forbidden) })
        {
            Assert.Equal(status, (await client.GetAsync("/api/compras")).StatusCode);
            Assert.Equal(status, (await client.GetAsync("/api/compras/1")).StatusCode);
            Assert.Equal(status, (await client.PostAsJsonAsync("/api/compras", datos)).StatusCode);
            Assert.Equal(status, (await client.PutAsJsonAsync("/api/compras/1", datos)).StatusCode);
            Assert.Equal(status, (await client.DeleteAsync("/api/compras/1")).StatusCode);
        }
    }
    [Fact] public async Task Recibida_ImpideEditarYEliminarYClienteNoDecideTotalesNiEstado()
    {
        using var factory = new SmokeFactory(); using var client = factory.CreateClient(); factory.InitializeSchema();
        await SmokeTests.Login(factory, client, "Administrador"); var datos = await Preparar(factory);
        var manipulado = new { datos.ProveedorId, datos.Fecha, datos.TipoCambio, datos.FletePen, datos.AduanaPen, datos.Lineas, estado = "Recibida", totalPen = .01m };
        var response = await client.PostAsJsonAsync("/api/compras", manipulado);
        var c = (await response.Content.ReadFromJsonAsync<CompraDto>())!; Assert.Equal("Borrador", c.Estado); Assert.Equal(91, c.TotalPen);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var entity = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.SingleAsync(db.Set<Compra>(), x => x.Id == c.Id);
            // Test fixture only: no production receipt transition is introduced.
            db.Entry(entity).Property(x => x.Estado).CurrentValue = EstadoCompra.Recibida; await db.SaveChangesAsync();
        }
        var edit = await client.PutAsJsonAsync($"/api/compras/{c.Id}", datos with { FletePen = 100 });
        Assert.Equal(HttpStatusCode.Conflict, edit.StatusCode); Assert.Equal("application/problem+json", edit.Content.Headers.ContentType!.MediaType);
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/compras/{c.Id}")).StatusCode);
        var stored = (await client.GetFromJsonAsync<CompraDto>($"/api/compras/{c.Id}"))!; Assert.Equal(91, stored.TotalPen); Assert.Equal("Recibida", stored.Estado);
        using var check = factory.Services.CreateScope(); var context = check.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(9, (await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.SingleAsync(context.Set<Inventario>())).Cantidad);
        Assert.Equal(0, (await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.SingleAsync(context.Set<Producto>())).CostoPromedio);
        var recibida = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.SingleAsync(context.Set<Compra>());
        context.Remove(recibida); await Assert.ThrowsAsync<ConflictoException>(() => context.SaveChangesAsync());
    }
    [Fact] public async Task Api_ValidacionesNoDejanComprasParciales()
    {
        using var factory = new SmokeFactory(); using var client = factory.CreateClient(); factory.InitializeSchema();
        await SmokeTests.Login(factory, client, "Administrador"); var datos = await Preparar(factory);
        foreach (var invalido in new[] { datos with { TipoCambio = 0 }, datos with { Lineas = [] }, datos with { Lineas = [new(datos.Lineas[0].ProductoId, 0, 1)] }, datos with { Lineas = [new(999, 1, 1)] }, datos with { FletePen = -1 } })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/compras", invalido)).StatusCode);
        var lista = await client.GetFromJsonAsync<ImportNet.Domain.Interfaces.Pagina<CompraDto>>("/api/compras"); Assert.Equal(0, lista!.Count);
    }
    [Fact] public async Task Administrador_CrudConTotalesConfirmadosYSwagger()
    {
        using var factory = new SmokeFactory(); using var client = factory.CreateClient(); factory.InitializeSchema();
        await SmokeTests.Login(factory, client, "Administrador"); var datos = await Preparar(factory);
        var response = await client.PostAsJsonAsync("/api/compras", datos); Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var c = (await response.Content.ReadFromJsonAsync<CompraDto>())!; Assert.Equal(91, c.TotalPen); Assert.Equal("Borrador", c.Estado);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(response.Headers.Location)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/compras")).StatusCode);
        var roundTrip = (await client.GetFromJsonAsync<CompraDatos>($"/api/compras/{c.Id}"))!;
        Assert.Equal(DateTimeKind.Utc, roundTrip.Fecha.Kind);
        var editada = await client.PutAsJsonAsync($"/api/compras/{c.Id}", roundTrip with { FletePen = 20 }); Assert.Equal(HttpStatusCode.OK, editada.StatusCode);
        Assert.Equal(101, (await editada.Content.ReadFromJsonAsync<CompraDto>())!.TotalPen);
        Assert.Contains("/api/compras", await client.GetStringAsync("/swagger/v1/swagger.json"));
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/compras/{c.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/compras/{c.Id}")).StatusCode);
    }
}
