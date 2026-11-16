using System.Net;
using System.Net.Http.Json;
using ImportNet.Application.UseCases.Terceros;
using ImportNet.Application.UseCases.Categorias.Commands;
using ImportNet.Application.UseCases.Productos.Commands;
using ImportNet.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ImportNet.Tests;

public sealed class TercerosTests
{
    [Theory]
    [InlineData(TipoDocumento.Dni, "01234567")]
    [InlineData(TipoDocumento.Ruc, "20123456789")]
    public async Task Cliente_BuscaDocumentoExactoConservaCerosYRechazaDuplicados(TipoDocumento tipo, string documento)
    {
        using var f = new ApplicationFixture();
        var id = await f.Mediator.Send(new GuardarClienteCommand(0, tipo, documento, "  Cliente prueba  ", "999888777"));
        var cliente = await f.Mediator.Send(new BuscarClientePorDocumentoQuery(documento));
        Assert.Equal(id, cliente.Id);
        Assert.Equal(documento, cliente.NumeroDocumento);
        Assert.Equal("Cliente prueba", cliente.Nombre);
        Assert.Equal("999888777", cliente.Telefono);
        await f.Mediator.Send(new GuardarClienteCommand(id, tipo, documento, "Cliente actualizado", null));
        await Assert.ThrowsAsync<ConflictoException>(() => f.Mediator.Send(new GuardarClienteCommand(0, tipo, documento, "Duplicado", null)));
        Assert.Equal(2, await f.Db.Set<Cliente>().CountAsync()); // Cliente varios + creado.
        await f.Mediator.Send(new EliminarClienteCommand(id));
        await Assert.ThrowsAsync<NoEncontradoException>(() => f.Mediator.Send(new BuscarClientePorDocumentoQuery(documento)));
    }

    [Theory]
    [InlineData(TipoDocumento.Dni, "1234567")]
    [InlineData(TipoDocumento.Dni, "123456789")]
    [InlineData(TipoDocumento.Ruc, "1234567890")]
    [InlineData(TipoDocumento.Ruc, "123456789012")]
    [InlineData(TipoDocumento.Dni, "1234567A")]
    [InlineData(TipoDocumento.Ruc, "2012345678A")]
    [InlineData(TipoDocumento.Dni, "１２３４５６７８")]
    [InlineData(TipoDocumento.Dni, "00000000")]
    [InlineData((TipoDocumento)99, "12345678901")]
    [InlineData(TipoDocumento.Dni, null)]
    public async Task Cliente_RechazaDocumentosInvalidosSinGuardar(TipoDocumento tipo, string? documento)
    {
        using var f = new ApplicationFixture();
        await Assert.ThrowsAsync<ReglaNegocioException>(() => f.Mediator.Send(new GuardarClienteCommand(0, tipo, documento!, "Cliente", null)));
        Assert.Single(await f.Db.Set<Cliente>().ToListAsync());
    }

    [Fact]
    public async Task Cliente_NoPermiteCambiarDocumentoPorOtroExistenteNiModificarSeed()
    {
        using var f = new ApplicationFixture();
        var id = await f.Mediator.Send(new GuardarClienteCommand(0, TipoDocumento.Dni, "12345678", "Primero", null));
        await f.Mediator.Send(new GuardarClienteCommand(0, TipoDocumento.Dni, "87654321", "Segundo", null));
        await Assert.ThrowsAsync<ConflictoException>(() => f.Mediator.Send(new GuardarClienteCommand(id, TipoDocumento.Dni, "87654321", "Primero", null)));
        var reservado = await f.Mediator.Send(new BuscarClientePorDocumentoQuery("00000000"));
        Assert.Equal("Cliente varios", reservado.Nombre);
        await Assert.ThrowsAsync<ConflictoException>(() => f.Mediator.Send(new GuardarClienteCommand(reservado.Id, TipoDocumento.Dni, "11111111", "Otro", null)));
        await Assert.ThrowsAsync<ConflictoException>(() => f.Mediator.Send(new EliminarClienteCommand(reservado.Id)));
    }

    [Fact]
    public async Task Proveedor_ReemplazaProductosYConservaContactoYEstado()
    {
        using var f = new ApplicationFixture();
        var categoria = await f.Mediator.Send(new CreateCategoriaCommand("Menaje"));
        var producto1 = await f.Mediator.Send(new CreateProductoCommand("P1", "Olla", null, "Unidad", null, 10, null, categoria, 0));
        var producto2 = await f.Mediator.Send(new CreateProductoCommand("P2", "Plato", null, "Unidad", null, 5, null, categoria, 0));
        var datos = new GuardarProveedorCommand(0, "Proveedor", TipoProveedor.Nacional, "Perú", "Ventas", true, [producto1]);
        var id = await f.Mediator.Send(datos);
        await f.Mediator.Send(datos with { Id = id, Activo = false, Contacto = "Compras", Productos = [producto2] });
        f.Db.ChangeTracker.Clear();
        var detalle = await f.Mediator.Send(new ObtenerProveedorQuery(id));
        Assert.False(detalle.Proveedor.Activo);
        Assert.Equal("Compras", detalle.Proveedor.Contacto);
        Assert.Equal(producto2, Assert.Single(detalle.Productos));
        await Assert.ThrowsAsync<ReglaNegocioException>(() => f.Mediator.Send(datos with { Productos = [999999] }));
        Assert.Single(await f.Uow.Proveedores.Proveedores("Proveedor", default));
        await f.Mediator.Send(new EliminarProveedorCommand(id));
        Assert.Empty(await f.Db.Set<ProveedorProducto>().ToListAsync());
    }

    [Fact]
    public async Task Api_VendedorRegistraYBuscaClientePeroNoRegistraProveedor()
    {
        using var factory = new SmokeFactory();
        using var vendedor = factory.CreateClient();
        using var anonimo = factory.CreateClient();
        factory.InitializeSchema();
        await SmokeTests.Login(factory, vendedor, "Vendedor");
        var cliente = new GuardarClienteCommand(0, TipoDocumento.Dni, "01234567", "Cliente vendedor", null);
        Assert.Equal(HttpStatusCode.Created, (await vendedor.PostAsJsonAsync("/api/clientes", cliente)).StatusCode);
        var resultado = await vendedor.GetFromJsonAsync<Cliente>("/api/clientes/por-documento/01234567");
        Assert.Equal("Cliente vendedor", resultado!.Nombre);
        Assert.Equal(HttpStatusCode.Conflict, (await vendedor.PostAsJsonAsync("/api/clientes", cliente)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await vendedor.GetAsync("/api/clientes/por-documento/11111111")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await vendedor.GetAsync("/api/clientes/por-documento/0123")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync("/api/clientes/por-documento/01234567")).StatusCode);
        var proveedor = new GuardarProveedorCommand(0, "Proveedor", TipoProveedor.Nacional, "Perú", "Ventas", true, []);
        Assert.Equal(HttpStatusCode.Forbidden, (await vendedor.PostAsJsonAsync("/api/proveedores", proveedor)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await vendedor.PutAsJsonAsync($"/api/clientes/{resultado.Id}", cliente)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await vendedor.DeleteAsync($"/api/clientes/{resultado.Id}")).StatusCode);
    }
}
