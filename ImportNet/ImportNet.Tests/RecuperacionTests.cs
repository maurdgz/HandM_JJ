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
    [Fact] public async Task Api_RolesCostosYClientes()
    {
        using var factory = new SmokeFactory(); using var anon = factory.CreateClient(); using var seller = factory.CreateClient(); using var admin = factory.CreateClient();
        factory.InitializeSchema();
        await SmokeTests.Login(factory, seller, "Vendedor"); await SmokeTests.Login(factory, admin, "Administrador");
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, (await seller.GetAsync("/api/proveedores")).StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, (await seller.GetAsync("/api/empleados")).StatusCode);
        var cliente = new GuardarClienteCommand(0, TipoDocumento.Dni, "87654321", "Local", null);
        Assert.Equal(System.Net.HttpStatusCode.Created, (await System.Net.Http.Json.HttpClientJsonExtensions.PostAsJsonAsync(seller, "/api/clientes", cliente)).StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.OK, (await seller.GetAsync("/api/clientes?texto=Local")).StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, (await System.Net.Http.Json.HttpClientJsonExtensions.PutAsJsonAsync(seller, "/api/clientes/2", cliente)).StatusCode);
        using var scope = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.CreateScope(factory.Services);
        var db = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<ImportNet.Infrastructure.Persistence.AppDbContext>(scope.ServiceProvider);
        var categoria = new Categoria("Menaje"); db.Add(categoria); await db.SaveChangesAsync();
        db.Add(new Producto("OLLA", "Olla", null, "Unidad", null, 15, null, categoria.Id, 0)); await db.SaveChangesAsync();
        var json = await seller.GetStringAsync("/api/productos"); Assert.DoesNotContain("costoPromedio", json); Assert.DoesNotContain("costoImportacionUsd", json);
    }
    [Fact] public async Task Catalogo_FiltraOllaPaginaYNoBorraMovimientos()
    {
        using var f = new ApplicationFixture();
        var cat = await f.Mediator.Send(new CreateCategoriaCommand("Menaje"));
        var id = await f.Mediator.Send(new CreateProductoCommand("OLLA", "Olla acero", null, "Unidad", null, 10, null, cat, 2));
        var p = await f.Mediator.Send(new BuscarProductosQuery("olla", cat, true, 1, 1));
        Assert.Equal(1, p.Count); Assert.Equal(id, Assert.Single(p.Items).Id);
        var usuario = new ImportNet.Infrastructure.Identity.ApplicationUser { Id = Guid.NewGuid().ToString(), UserName = "movimiento@example.test", Email = "movimiento@example.test" };
        f.Db.Add(usuario);
        var empleado = new Empleado { UsuarioId = usuario.Id, Nombre = "Prueba", Apellidos = "Movimiento", Cargo = "Administrador", Area = "QA" };
        f.Db.Add(empleado); await f.Db.SaveChangesAsync();
        f.Db.Add(new MovimientoInventario { ProductoId = id, AlmacenId = Almacen.PrincipalId, EmpleadoId = empleado.Id, Tipo = TipoMovimiento.Entrada, Cantidad = 1 });
        await f.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<ConflictoException>(() => f.Mediator.Send(new DeleteProductoCommand(id)));
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
