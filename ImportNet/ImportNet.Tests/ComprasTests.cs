using ImportNet.Application.UseCases.Compras;
using ImportNet.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace ImportNet.Tests;
public sealed class ComprasTests
{
    internal static async Task<CompraDatos> Datos(ApplicationFixture f)
    {
        var categoria = new Categoria("Menaje"); var proveedor = new Proveedor { Nombre = "Fabrica", Pais = "China", Contacto = "Ventas" };
        f.Db.AddRange(categoria, proveedor); await f.Db.SaveChangesAsync();
        var p = new Producto("OLLA", "Olla", null, "Unidad", null, 10, null, categoria.Id, 2);
        f.Db.Add(p); await f.Db.SaveChangesAsync();
        f.Db.Add(new Inventario { ProductoId = p.Id, AlmacenId = Almacen.PrincipalId, Cantidad = 7 }); await f.Db.SaveChangesAsync();
        return new(proveedor.Id, DateTime.UtcNow, 3.75m, 10, 5, [new(p.Id, 2, 10)]);
    }
    [Fact] public async Task Crear_AtomicoConTotalesYConsultas()
    {
        using var f = new ApplicationFixture(); var datos = await Datos(f);
        var compra = await f.Mediator.Send(new CrearCompraCommand(datos));
        Assert.Equal("Borrador", compra.Estado); Assert.Equal(90, compra.TotalPen);
        f.Db.ChangeTracker.Clear();
        Assert.Equal(90, (await f.Mediator.Send(new ObtenerCompraQuery(compra.Id))).TotalPen);
        var lista = await f.Mediator.Send(new ListarComprasQuery()); Assert.Equal(1, lista.Count); Assert.Single(lista.Items);
        await Assert.ThrowsAsync<ReglaNegocioException>(() => f.Mediator.Send(new CrearCompraCommand(datos with { ProveedorId = 999 })));
        await Assert.ThrowsAsync<ReglaNegocioException>(() => f.Mediator.Send(new CrearCompraCommand(datos with { Lineas = [new(999, 1, 1)] })));
        Assert.Equal(1, await f.Db.Set<Compra>().CountAsync()); Assert.Equal(7, (await f.Db.Set<Inventario>().SingleAsync()).Cantidad);
        Assert.Equal(0, (await f.Db.Set<Producto>().SingleAsync()).CostoPromedio); Assert.Empty(await f.Db.Set<MovimientoInventario>().ToListAsync());
    }
}
