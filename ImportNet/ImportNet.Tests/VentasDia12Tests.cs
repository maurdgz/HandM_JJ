using System.Net;
using System.Net.Http.Json;
using ImportNet.Application.UseCases.Ventas;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ImportNet.Tests;

internal static class VentasDatosPrueba
{
    public static async Task<int[]> Catalogo(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var unique = Guid.NewGuid().ToString("N");
        var categoria = new Categoria("Ventas-" + unique);
        db.Add(categoria);
        await db.SaveChangesAsync();
        var a = new Producto("A-" + unique, "Olla de prueba", null, "Unidad", null, 10m, null, categoria.Id, 0);
        var b = new Producto("B-" + unique, "Organizador de prueba", null, "Unidad", null, 7m, null, categoria.Id, 0);
        db.AddRange(a, b);
        await db.SaveChangesAsync();
        db.AddRange(
            new Inventario { ProductoId = a.Id, AlmacenId = Almacen.PrincipalId, Cantidad = 5m },
            new Inventario { ProductoId = b.Id, AlmacenId = Almacen.PrincipalId, Cantidad = 9m });
        await db.SaveChangesAsync();
        return [a.Id, b.Id];
    }

    public static async Task<VentaRegistradaDto> Registrar(HttpClient client, int[] productos)
    {
        using var response = await client.PostAsJsonAsync("/api/ventas", new {
            clienteId = 1,
            lineas = new[] {
                new { productoId = productos[0], cantidad = 2m, descuento = 0m },
                new { productoId = productos[1], cantidad = 1m, descuento = 0m }
            }
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        return (await response.Content.ReadFromJsonAsync<VentaRegistradaDto>())!;
    }

    public static async Task ComprobarReposicion(IServiceProvider services, int ventaId, int[] productos)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(5m, (await db.Set<Inventario>().SingleAsync(i => i.ProductoId == productos[0])).Cantidad);
        Assert.Equal(9m, (await db.Set<Inventario>().SingleAsync(i => i.ProductoId == productos[1])).Cantidad);
        var entradas = await db.Set<MovimientoInventario>().Where(m => m.VentaId == ventaId && m.Tipo == TipoMovimiento.Entrada).ToListAsync();
        Assert.Equal(2, entradas.Count);
        Assert.Equal(3m, entradas.Sum(m => m.Cantidad));
        Assert.All(entradas, m => Assert.Equal(Almacen.PrincipalId, m.AlmacenId));
        Assert.Equal(2, await db.Set<MovimientoInventario>().CountAsync(m => m.VentaId == ventaId && m.Tipo == TipoMovimiento.Salida));
        var venta = await db.Set<Venta>().SingleAsync(v => v.Id == ventaId);
        Assert.Equal(EstadoVenta.Anulada, venta.Estado);
        Assert.NotNull(venta.FechaAnulacion);
        Assert.All(await db.Set<Producto>().Where(p => productos.Contains(p.Id)).ToListAsync(), p => Assert.Equal(0m, p.CostoPromedio));
    }
}

public sealed class VentasDia12Tests
{
    [Fact]
    public async Task PosHistorialNotaYAnulacion_RespetanPropietarioYRoles()
    {
        using var factory = new SmokeFactory();
        using var admin = factory.CreateClient();
        using var vendedor = factory.CreateClient();
        using var otro = factory.CreateClient();
        using var anon = factory.CreateClient();
        factory.InitializeSchema();
        await SmokeTests.Login(factory, admin, "Administrador");
        await SmokeTests.Login(factory, vendedor, "Vendedor");
        await SmokeTests.Login(factory, otro, "Vendedor");
        var productos = await VentasDatosPrueba.Catalogo(factory.Services);
        var venta = await VentasDatosPrueba.Registrar(vendedor, productos);
        Assert.Equal(27m, venta.Total);
        await VentasDatosPrueba.Registrar(admin, await VentasDatosPrueba.Catalogo(factory.Services));

        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync("/api/ventas")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync($"/api/ventas/{venta.Id}")).StatusCode);
        Assert.Contains((await admin.GetFromJsonAsync<Pagina<ResumenVenta>>("/api/ventas"))!.Items, v => v.Id == venta.Id);
        Assert.Single((await vendedor.GetFromJsonAsync<Pagina<ResumenVenta>>("/api/ventas"))!.Items);
        Assert.Empty((await otro.GetFromJsonAsync<Pagina<ResumenVenta>>("/api/ventas?empleadoId=1"))!.Items);
        var primera = (await admin.GetFromJsonAsync<Pagina<ResumenVenta>>("/api/ventas?pagina=1&tamano=1"))!;
        var segunda = (await admin.GetFromJsonAsync<Pagina<ResumenVenta>>("/api/ventas?pagina=2&tamano=1"))!;
        Assert.Equal(2, primera.Count);
        Assert.Single(primera.Items);
        Assert.Single(segunda.Items);
        Assert.NotEqual(primera.Items[0].Id, segunda.Items[0].Id);
        Assert.Equal(HttpStatusCode.Forbidden, (await otro.GetAsync($"/api/ventas/{venta.Id}")).StatusCode);
        var nota = (await vendedor.GetFromJsonAsync<NotaVenta>($"/api/ventas/{venta.Id}"))!;
        Assert.Equal(venta.Numero, nota.Numero);
        Assert.Equal(DateTimeKind.Utc, nota.Fecha.Kind);
        Assert.False(string.IsNullOrWhiteSpace(nota.Vendedor));
        Assert.Equal("Cliente varios", nota.Cliente);
        Assert.Equal(2, nota.Lineas.Count);
        Assert.Equal(27m, nota.Total);
        var json = await vendedor.GetStringAsync($"/api/ventas/{venta.Id}");
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("costo", json, StringComparison.OrdinalIgnoreCase);

        var ruta = $"/api/ventas/{venta.Id}/anulacion";
        Assert.Equal(HttpStatusCode.Forbidden, (await vendedor.PostAsJsonAsync(ruta, new { motivo = "Devolución" })).StatusCode);
        foreach (var motivo in new[] { " ", new string('x', 301) })
            Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync(ruta, new { motivo })).StatusCode);
        var anulacion = await admin.PostAsJsonAsync(ruta, new { motivo = "Devolución verificada" });
        Assert.Equal(HttpStatusCode.OK, anulacion.StatusCode);
        var resultado = (await anulacion.Content.ReadFromJsonAsync<VentaAnuladaDto>())!;
        Assert.Equal(venta.Id, resultado.Id);
        Assert.Equal("Anulada", resultado.Estado);
        Assert.Equal("Devolución verificada", resultado.Motivo);
        Assert.Equal(2, resultado.Reposiciones.Count);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(ruta, new { motivo = "Segundo intento" })).StatusCode);
        await VentasDatosPrueba.ComprobarReposicion(factory.Services, venta.Id, productos);
        Assert.Equal("Anulada", (await vendedor.GetFromJsonAsync<NotaVenta>($"/api/ventas/{venta.Id}"))!.Estado);
    }

    [Fact]
    public async Task Historial_UsaDiasPeruanosYValidaElPeriodo()
    {
        using var factory = new SmokeFactory();
        using var seller = factory.CreateClient();
        factory.InitializeSchema();
        await SmokeTests.Login(factory, seller, "Vendedor");
        var productos = await VentasDatosPrueba.Catalogo(factory.Services);
        var venta = await VentasDatosPrueba.Registrar(seller, productos);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.Set<Venta>().SingleAsync(v => v.Id == venta.Id)).Fecha = new DateTime(2026, 11, 20, 4, 59, 59, DateTimeKind.Utc);
            await db.SaveChangesAsync();
        }
        Assert.Single((await seller.GetFromJsonAsync<Pagina<ResumenVenta>>("/api/ventas?desde=2026-11-19&hasta=2026-11-19"))!.Items);
        Assert.Empty((await seller.GetFromJsonAsync<Pagina<ResumenVenta>>("/api/ventas?desde=2026-11-20&hasta=2026-11-20"))!.Items);
        Assert.Equal(HttpStatusCode.BadRequest, (await seller.GetAsync("/api/ventas?desde=2026-11-21&hasta=2026-11-20")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await seller.GetAsync("/api/ventas?desde=incorrecto")).StatusCode);
        foreach (var query in new[] { "hasta=9999-12-31", "desde=2025-01-01&hasta=2026-11-20", "pagina=0", "tamano=101" })
            Assert.Equal(HttpStatusCode.BadRequest, (await seller.GetAsync("/api/ventas?" + query)).StatusCode);
    }

    [Fact]
    public async Task Anulacion_FalloDePersistenciaRevierteTodoYPermiteReintentar()
    {
        using var factory = new SmokeFactory();
        using var admin = factory.CreateClient();
        factory.InitializeSchema();
        await SmokeTests.Login(factory, admin, "Administrador");
        var productos = await VentasDatosPrueba.Catalogo(factory.Services);
        var venta = await VentasDatosPrueba.Registrar(admin, productos);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER fallo_entrada_venta BEFORE INSERT ON MovimientoInventario WHEN NEW.Tipo = 0 BEGIN SELECT RAISE(ABORT, 'Fallo de prueba'); END;");
        }
        var ruta = $"/api/ventas/{venta.Id}/anulacion";
        using var failed = await admin.PostAsJsonAsync(ruta, new { motivo = "Prueba rollback" });
        Assert.Equal(HttpStatusCode.InternalServerError, failed.StatusCode);
        Assert.Equal("application/problem+json", failed.Content.Headers.ContentType!.MediaType);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(EstadoVenta.Registrada, (await db.Set<Venta>().SingleAsync(v => v.Id == venta.Id)).Estado);
            Assert.Equal(3m, (await db.Set<Inventario>().SingleAsync(i => i.ProductoId == productos[0])).Cantidad);
            Assert.Equal(8m, (await db.Set<Inventario>().SingleAsync(i => i.ProductoId == productos[1])).Cantidad);
            Assert.Equal(0, await db.Set<MovimientoInventario>().CountAsync(m => m.VentaId == venta.Id && m.Tipo == TipoMovimiento.Entrada));
            await db.Database.ExecuteSqlRawAsync("DROP TRIGGER fallo_entrada_venta;");
        }
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync(ruta, new { motivo = "Reintento" })).StatusCode);
        await VentasDatosPrueba.ComprobarReposicion(factory.Services, venta.Id, productos);
    }
}
