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
