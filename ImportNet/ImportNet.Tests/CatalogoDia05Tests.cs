using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ImportNet.Application.UseCases.Categorias.Commands;
using ImportNet.Application.UseCases.Productos.Commands;
using ImportNet.Application.UseCases.Productos.Queries;
using ImportNet.Domain.Entities;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ImportNet.Tests;

public sealed class CatalogoDia05Tests
{
    [Fact]
    public async Task ProductoConMovimientos_NoSeEliminaPeroSeDesactivaYReactiva()
    {
        using var f = new ApplicationFixture();
        var cat = await f.Mediator.Send(new CreateCategoriaCommand("Menaje"));
        var id = await f.Mediator.Send(new CreateProductoCommand("OLLA-01", "Olla acero", null, "Unidad", null, 10, null, cat, 2));
        await RegistrarMovimiento(f.Db, id);

        await Assert.ThrowsAsync<ConflictoException>(() => f.Mediator.Send(new DeleteProductoCommand(id)));
        await f.Mediator.Send(new CambiarEstadoProductoCommand(id, false));
        Assert.False((await f.Db.Set<Producto>().AsNoTracking().SingleAsync(p => p.Id == id)).Activo);
        await f.Mediator.Send(new CambiarEstadoProductoCommand(id, true));
        Assert.True((await f.Db.Set<Producto>().AsNoTracking().SingleAsync(p => p.Id == id)).Activo);
        await Assert.ThrowsAsync<NoEncontradoException>(() => f.Mediator.Send(new CambiarEstadoProductoCommand(9999, false)));
    }

    [Fact]
    public async Task Api_SoloAdministradorCambiaEstadoDeProducto()
    {
        using var factory = new SmokeFactory();
        using var anon = factory.CreateClient(); using var seller = factory.CreateClient(); using var admin = factory.CreateClient();
        factory.InitializeSchema();
        await SmokeTests.Login(factory, seller, "Vendedor"); await SmokeTests.Login(factory, admin, "Administrador");
        var id = await CrearProductoEnBd(factory, "OLLA-01", "Olla acero");

        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.PatchAsJsonAsync($"/api/productos/{id}/estado", new { activo = false })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await seller.PatchAsJsonAsync($"/api/productos/{id}/estado", new { activo = false })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PatchAsJsonAsync($"/api/productos/{id}/estado", new { activo = false })).StatusCode);
        using var json = JsonDocument.Parse(await admin.GetStringAsync($"/api/productos/{id}"));
        Assert.False(json.RootElement.GetProperty("activo").GetBoolean());
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PatchAsJsonAsync("/api/productos/9999/estado", new { activo = true })).StatusCode);
    }

    [Fact]
    public async Task Buscar_FiltraPorEstadoActivo()
    {
        using var f = new ApplicationFixture();
        var cat = await f.Mediator.Send(new CreateCategoriaCommand("Menaje"));
        var activa = await f.Mediator.Send(new CreateProductoCommand("OLLA-01", "Olla acero", null, "Unidad", null, 10, null, cat, 0));
        var inactiva = await f.Mediator.Send(new CreateProductoCommand("OLLA-02", "Olla barro", null, "Unidad", null, 10, null, cat, 0));
        await f.Mediator.Send(new CambiarEstadoProductoCommand(inactiva, false));

        Assert.Equal(2, (await f.Mediator.Send(new BuscarProductosQuery("olla"))).Count);
        Assert.Equal(activa, Assert.Single((await f.Mediator.Send(new BuscarProductosQuery("olla", Activo: true))).Items).Id);
        Assert.Equal(inactiva, Assert.Single((await f.Mediator.Send(new BuscarProductosQuery("olla", Activo: false))).Items).Id);
    }

    [Fact]
    public async Task Api_VendedorSoloVeProductosActivosAunquePidaInactivos()
    {
        using var factory = new SmokeFactory();
        using var seller = factory.CreateClient(); using var admin = factory.CreateClient();
        factory.InitializeSchema();
        await SmokeTests.Login(factory, seller, "Vendedor"); await SmokeTests.Login(factory, admin, "Administrador");
        var activa = await CrearProductoEnBd(factory, "OLLA-01", "Olla acero");
        var inactiva = await CrearProductoEnBd(factory, "OLLA-02", "Olla barro");
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PatchAsJsonAsync($"/api/productos/{inactiva}/estado", new { activo = false })).StatusCode);

        Assert.Equal([activa], await Ids(seller, "/api/productos/buscar?texto=olla"));
        Assert.Equal([activa], await Ids(seller, "/api/productos/buscar?texto=olla&activo=false"));
        Assert.Equal([inactiva], await Ids(admin, "/api/productos/buscar?texto=olla&activo=false"));
        Assert.Equal(2, (await Ids(admin, "/api/productos/buscar?texto=olla")).Count);
    }

    [Fact]
    public async Task Buscar_FiltraTextoSkuCategoriaYStockBajo()
    {
        using var f = new ApplicationFixture();
        var menaje = await f.Mediator.Send(new CreateCategoriaCommand("Menaje"));
        var juguetes = await f.Mediator.Send(new CreateCategoriaCommand("Juguetes"));
        var olla = await f.Mediator.Send(new CreateProductoCommand("MEN-001", "Olla de acero 24 cm", null, "Unidad", null, 45, null, menaje, 5));
        var ollaExpress = await f.Mediator.Send(new CreateProductoCommand("MEN-002", "OLLA express", null, "Unidad", null, 120, null, menaje, 2));
        await f.Mediator.Send(new CreateProductoCommand("MEN-003", "Sartén antiadherente", null, "Unidad", null, 35, null, menaje, 2));
        await f.Mediator.Send(new CreateProductoCommand("JUG-001", "Carrito a fricción", null, "Unidad", null, 12, null, juguetes, 3));
        // Olla express has stock above its minimum; Olla 24 cm has none.
        f.Db.Add(new Inventario { ProductoId = ollaExpress, AlmacenId = Almacen.PrincipalId, Cantidad = 10 });
        await f.Db.SaveChangesAsync();

        var texto = await f.Mediator.Send(new BuscarProductosQuery("olla"));
        Assert.Equal(2, texto.Count);
        Assert.Equal([olla, ollaExpress], texto.Items.Select(p => p.Id).Order());
        Assert.Equal("Sartén antiadherente", Assert.Single((await f.Mediator.Send(new BuscarProductosQuery("men-003"))).Items).Nombre);
        Assert.Equal(3, (await f.Mediator.Send(new BuscarProductosQuery(CategoriaId: menaje))).Count);
        Assert.Equal(olla, Assert.Single((await f.Mediator.Send(new BuscarProductosQuery("olla", StockBajo: true))).Items).Id);
        Assert.Equal(0, (await f.Mediator.Send(new BuscarProductosQuery("olla", juguetes))).Count);
    }

    [Fact]
    public async Task Buscar_PaginaConConteoTotalYRechazaPaginacionInvalida()
    {
        using var f = new ApplicationFixture();
        var cat = await f.Mediator.Send(new CreateCategoriaCommand("Organizadores"));
        for (var i = 1; i <= 5; i++)
            await f.Mediator.Send(new CreateProductoCommand($"ORG-{i:00}", $"Organizador {i}", null, "Unidad", null, 10, null, cat, 0));

        var p1 = await f.Mediator.Send(new BuscarProductosQuery(Pagina: 1, Tamano: 2));
        var p3 = await f.Mediator.Send(new BuscarProductosQuery(Pagina: 3, Tamano: 2));
        Assert.Equal(5, p1.Count);
        Assert.Equal(["Organizador 1", "Organizador 2"], p1.Items.Select(p => p.Nombre));
        Assert.Equal("Organizador 5", Assert.Single(p3.Items).Nombre);
        Assert.Empty((await f.Mediator.Send(new BuscarProductosQuery(Pagina: 4, Tamano: 2))).Items);
        await Assert.ThrowsAsync<ReglaNegocioException>(() => f.Mediator.Send(new BuscarProductosQuery(Pagina: 0)));
        await Assert.ThrowsAsync<ReglaNegocioException>(() => f.Mediator.Send(new BuscarProductosQuery(Tamano: 101)));
    }

    [Fact]
    public async Task Api_BusquedaOcultaCostosAlVendedorYLosMuestraAlAdministrador()
    {
        using var factory = new SmokeFactory();
        using var seller = factory.CreateClient(); using var admin = factory.CreateClient();
        factory.InitializeSchema();
        await SmokeTests.Login(factory, seller, "Vendedor"); await SmokeTests.Login(factory, admin, "Administrador");
        await CrearProductoEnBd(factory, "OLLA-01", "Olla acero");

        var vendedor = await seller.GetStringAsync("/api/productos/buscar?texto=olla");
        Assert.Contains("precioVenta", vendedor);
        Assert.DoesNotContain("costoPromedio", vendedor);
        Assert.DoesNotContain("costoImportacionUsd", vendedor);
        Assert.DoesNotContain("tipoCambioImportacion", vendedor);
        Assert.Contains("costoPromedio", await admin.GetStringAsync("/api/productos/buscar?texto=olla"));
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync("/api/productos/buscar?pagina=0")).StatusCode);
    }

    internal static async Task<List<int>> Ids(HttpClient client, string url)
    {
        using var json = JsonDocument.Parse(await client.GetStringAsync(url));
        return json.RootElement.GetProperty("items").EnumerateArray().Select(x => x.GetProperty("id").GetInt32()).ToList();
    }

    internal static async Task RegistrarMovimiento(AppDbContext db, int productoId)
    {
        var usuario = new ImportNet.Infrastructure.Identity.ApplicationUser { Id = Guid.NewGuid().ToString(), UserName = $"{Guid.NewGuid():N}@example.test", Email = $"{Guid.NewGuid():N}@example.test" };
        db.Add(usuario);
        var empleado = new Empleado { UsuarioId = usuario.Id, Nombre = "Prueba", Apellidos = "Movimiento", Cargo = "Administrador", Area = "QA" };
        db.Add(empleado); await db.SaveChangesAsync();
        db.Add(new MovimientoInventario { ProductoId = productoId, AlmacenId = Almacen.PrincipalId, EmpleadoId = empleado.Id, Tipo = TipoMovimiento.Entrada, Cantidad = 1 });
        await db.SaveChangesAsync();
    }

    internal static async Task<int> CrearProductoEnBd(SmokeFactory factory, string codigo, string nombre, int? categoriaId = null)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (categoriaId is null) { var c = new Categoria($"Cat {codigo}"); db.Add(c); await db.SaveChangesAsync(); categoriaId = c.Id; }
        var p = new Producto(codigo, nombre, null, "Unidad", null, 15, null, categoriaId.Value, 0);
        db.Add(p); await db.SaveChangesAsync();
        return p.Id;
    }
}
