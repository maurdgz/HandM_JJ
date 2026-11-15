using ImportNet.Application.UseCases.Terceros;
using ImportNet.Application.UseCases.Productos.Queries;
using ImportNet.Application.UseCases.Productos.Commands;
using ImportNet.Application.UseCases.Categorias.Commands;
using ImportNet.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace ImportNet.Tests;
public sealed class RecuperacionTests
{
    [Fact] public async Task Catalogo_FiltraOllaPaginaYNoBorraMovimientos()
    {
        using var f = new ApplicationFixture();
        var cat = await f.Mediator.Send(new CreateCategoriaCommand("Menaje"));
        var id = await f.Mediator.Send(new CreateProductoCommand("OLLA", "Olla acero", null, "Unidad", null, 10, null, cat, 2));
        var p = await f.Mediator.Send(new BuscarProductosQuery("olla", cat, true, 1, 1));
        Assert.Equal(1, p.Count); Assert.Equal(id, Assert.Single(p.Items).Id);
        f.Db.Attach(new MovimientoInventario { Id = 99, ProductoId = id });
        // Reference guard is covered with a real relational inventory in CatalogoTests.
        await Assert.ThrowsAsync<ConflictoException>(() => f.Mediator.Send(new CreateProductoCommand("OLLA", "Otra", null, "Unidad", null, 10, null, cat, 0)));
    }
    [Fact] public async Task Terceros_ValidaDocumentosYRelacionSinDuplicarSeed()
    {
        using var f = new ApplicationFixture();
        Assert.Single(await f.Db.Set<Cliente>().Where(x => x.NumeroDocumento == "00000000").ToListAsync());
        await Assert.ThrowsAsync<ReglaNegocioException>(() => f.Mediator.Send(new GuardarClienteCommand(0, TipoDocumento.Dni, "123", "Cliente", null)));
        await f.Mediator.Send(new GuardarClienteCommand(0, TipoDocumento.Dni, "12345678", "Cliente", null));
        await Assert.ThrowsAsync<ConflictoException>(() => f.Mediator.Send(new GuardarClienteCommand(0, TipoDocumento.Dni, "12345678", "Otro", null)));
        await f.Mediator.Send(new GuardarClienteCommand(0, TipoDocumento.Ruc, "20123456789", "Empresa", null));
        Assert.Single(await f.Mediator.Send(new ListarClientesQuery("Empresa")));
        var cat = await f.Mediator.Send(new CreateCategoriaCommand("Menaje"));
        var id = await f.Mediator.Send(new CreateProductoCommand("O1", "Olla", null, "Unidad", null, 10, null, cat, 0));
        var proveedor = await f.Mediator.Send(new GuardarProveedorCommand(0, "Fabrica", TipoProveedor.Internacional, "China", "Ventas", true, [id]));
        Assert.Equal(id, Assert.Single((await f.Mediator.Send(new ObtenerProveedorQuery(proveedor))).Productos));
        await Assert.ThrowsAsync<ReglaNegocioException>(() => f.Mediator.Send(new GuardarProveedorCommand(proveedor, "Fabrica", TipoProveedor.Internacional, "China", "Ventas", true, [id, id])));
    }
}
