using ImportNet.Application.UseCases.Categorias.Commands;
using ImportNet.Application.UseCases.Categorias.Queries;
using ImportNet.Application.UseCases.Productos.Commands;
using ImportNet.Application.UseCases.Productos.Queries;
using ImportNet.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ImportNet.Tests;

public sealed class CatalogoTests
{
    [Fact]
    public async Task Categoria_CicloCompleto()
    {
        using var f = new ApplicationFixture();
        var id = await f.Mediator.Send(new CreateCategoriaCommand("  Electrónica  "));
        Assert.True(id > 0);
        Assert.Equal("Electrónica", (await f.Mediator.Send(new GetCategoriaByIdQuery(id))).Nombre);
        await f.Mediator.Send(new UpdateCategoriaCommand(id, "Accesorios"));
        Assert.Single(await f.Mediator.Send(new GetAllCategoriasQuery()));
        Assert.Equal("Accesorios", (await f.Mediator.Send(new GetCategoriaByIdQuery(id))).Nombre);
        await f.Mediator.Send(new DeleteCategoriaCommand(id));
        Assert.Empty(await f.Mediator.Send(new GetAllCategoriasQuery()));
        await Assert.ThrowsAsync<NoEncontradoException>(() => f.Mediator.Send(new GetCategoriaByIdQuery(id)));
    }
    [Fact]
    public async Task Categoria_ValidaDuplicadosYNombre()
    {
        using var f = new ApplicationFixture();
        await Assert.ThrowsAsync<ReglaNegocioException>(() => f.Mediator.Send(new CreateCategoriaCommand(" ")));
        await f.Mediator.Send(new CreateCategoriaCommand("Ropa"));
        await Assert.ThrowsAsync<ConflictoException>(() => f.Mediator.Send(new CreateCategoriaCommand("ropa")));
    }
    [Fact]
    public async Task Producto_CicloCompletoYRestriccionCategoria()
    {
        using var f = new ApplicationFixture();
        var categoria = await f.Mediator.Send(new CreateCategoriaCommand("Ropa"));
        var id = await f.Mediator.Send(CrearProducto(categoria));
        var item = await f.Mediator.Send(new GetProductoByIdQuery(id));
        Assert.Equal("SKU-1", item.Codigo);
        Assert.Equal(12.50m, item.PrecioUsd);
        Assert.Single(await f.Mediator.Send(new GetAllProductosQuery()));
        await Assert.ThrowsAsync<ConflictoException>(() => f.Mediator.Send(new DeleteCategoriaCommand(categoria)));
        await f.Mediator.Send(new UpdateProductoCommand(id, "sku-2", "Nuevo", null, "Unidad", 2m, 20m, "123456", categoria, 4m));
        Assert.Equal("Nuevo", (await f.Mediator.Send(new GetProductoByIdQuery(id))).Nombre);
        await f.Mediator.Send(new DeleteProductoCommand(id));
        Assert.Empty(await f.Mediator.Send(new GetAllProductosQuery()));
        await f.Mediator.Send(new DeleteCategoriaCommand(categoria));
    }
    [Theory]
    [InlineData(0, 1, 0)]
    [InlineData(-1, 1, 0)]
    [InlineData(1, -1, 0)]
    [InlineData(1, 1, -1)]
    [InlineData(1.0001, 1, 0)]
    [InlineData(1, 1.001, 0)]
    public async Task Producto_RechazaMagnitudesInvalidas(decimal peso, decimal precio, decimal minimo)
    {
        using var f = new ApplicationFixture();
        var categoria = await f.Mediator.Send(new CreateCategoriaCommand("Ropa"));
        await Assert.ThrowsAsync<ReglaNegocioException>(() => f.Mediator.Send(CrearProducto(categoria) with { Peso = peso, PrecioUsd = precio, StockMinimo = minimo }));
        Assert.Empty(await f.Mediator.Send(new GetAllProductosQuery()));
    }
    [Fact]
    public async Task Producto_RechazaCategoriaInexistenteYCodigoDuplicado()
    {
        using var f = new ApplicationFixture();
        await Assert.ThrowsAsync<ReglaNegocioException>(() => f.Mediator.Send(CrearProducto(999)));
        var categoria = await f.Mediator.Send(new CreateCategoriaCommand("Ropa"));
        await f.Mediator.Send(CrearProducto(categoria));
        await Assert.ThrowsAsync<ConflictoException>(() => f.Mediator.Send(CrearProducto(categoria)));
    }
    [Fact]
    public async Task Producto_NoSeEliminaConInventario()
    {
        using var f = new ApplicationFixture();
        var categoria = await f.Mediator.Send(new CreateCategoriaCommand("Ropa"));
        var producto = await f.Mediator.Send(CrearProducto(categoria));
        var almacen = new Almacen { Nombre = "Principal" };
        f.Db.Add(almacen); await f.Db.SaveChangesAsync();
        f.Db.Add(new Inventario { ProductoId = producto, AlmacenId = almacen.Id, Cantidad = 1 });
        await f.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<ConflictoException>(() => f.Mediator.Send(new DeleteProductoCommand(producto)));
    }
    [Fact]
    public async Task RestriccionesRelacionales_ImpidenCodigoDuplicadoYCantidadNegativa()
    {
        using var f = new ApplicationFixture();
        var categoria = await f.Mediator.Send(new CreateCategoriaCommand("Ropa"));
        await f.Mediator.Send(CrearProducto(categoria));
        f.Db.Add(new Producto("SKU-1", "Otro", null, "Unidad", 1, 2, "123", categoria, 0));
        await Assert.ThrowsAsync<DbUpdateException>(() => f.Db.SaveChangesAsync());
        f.Db.ChangeTracker.Clear();
        var producto = (await f.Mediator.Send(new GetAllProductosQuery())).Single();
        var almacen = new Almacen { Nombre = "Principal" }; f.Db.Add(almacen); await f.Db.SaveChangesAsync();
        f.Db.Add(new Inventario { ProductoId = producto.Id, AlmacenId = almacen.Id, Cantidad = -1 });
        await Assert.ThrowsAsync<DbUpdateException>(() => f.Db.SaveChangesAsync());
    }
    [Fact]
    public void Movimiento_NoAdmiteModificacionNiBorrado()
    {
        using var f = new ApplicationFixture();
        var movimiento = new MovimientoInventario { Id = 1, Cantidad = 2 };
        f.Db.Attach(movimiento); movimiento.Cantidad = 3;
        Assert.Throws<ReglaNegocioException>(() => f.Db.SaveChanges());
        f.Db.Remove(movimiento);
        Assert.Throws<ReglaNegocioException>(() => f.Db.SaveChanges());
    }
    [Fact]
    public async Task Movimiento_InmutabilidadCubreSobrecargaAsync()
    {
        using var f = new ApplicationFixture();
        var movimiento = new MovimientoInventario { Id = 1, Cantidad = 2 };
        f.Db.Attach(movimiento); movimiento.Cantidad = 3;
        await Assert.ThrowsAsync<ReglaNegocioException>(() => f.Db.SaveChangesAsync(true));
        await Assert.ThrowsAsync<ReglaNegocioException>(() => f.Db.SaveChangesAsync());
    }
    [Fact]
    public async Task Producto_NoSeEliminaConRelacionProveedor()
    {
        using var f = new ApplicationFixture();
        var categoria = await f.Mediator.Send(new CreateCategoriaCommand("Accesorios"));
        var producto = await f.Mediator.Send(CrearProducto(categoria));
        var proveedor = new Proveedor { Nombre = "Proveedor Corea", Pais = "Corea del Sur", Tipo = TipoProveedor.Internacional, Contacto = "Comercial" };
        f.Db.Add(proveedor); await f.Db.SaveChangesAsync();
        f.Db.Add(new ProveedorProducto { ProveedorId = proveedor.Id, ProductoId = producto });
        await f.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<ConflictoException>(() => f.Mediator.Send(new DeleteProductoCommand(producto)));
    }
    [Fact]
    public void Orden_ValidaTransicionesYNoPermiteAnularDosVeces()
    {
        var orden = new OrdenVenta();
        Assert.Throws<ReglaNegocioException>(() => orden.CambiarEstado(EstadoOrden.Entregado));
        orden.CambiarEstado(EstadoOrden.Anulado);
        Assert.Throws<ReglaNegocioException>(() => orden.CambiarEstado(EstadoOrden.Anulado));
        var completa = new OrdenVenta();
        completa.CambiarEstado(EstadoOrden.Despachado);
        completa.CambiarEstado(EstadoOrden.Enviado);
        completa.CambiarEstado(EstadoOrden.Entregado);
        Assert.Throws<ReglaNegocioException>(() => completa.CambiarEstado(EstadoOrden.Anulado));
    }
    private static CreateProductoCommand CrearProducto(int categoria)
        => new("sku-1", "Camiseta", null, "Unidad", 0.250m, 12.50m, "610910", categoria, 2m);
}
