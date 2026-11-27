using ImportNet.Domain.Entities;
using Xunit;

namespace ImportNet.Tests;

public sealed class UnitOfWorkRepositoryTests
{
    [Fact]
    public async Task UnitOfWork_InstanciaRepositoriosLazyYConsultaPorLote()
    {
        using var fixture = new ApplicationFixture();

        var c1 = new Categoria("Hogar");
        fixture.Uow.Categorias.Agregar(c1);
        await fixture.Uow.GuardarAsync();

        var p1 = new Producto("P-01", "Olla", null, "Unidad", null, 80m, null, c1.Id, 5m);
        var p2 = new Producto("P-02", "Sartén", null, "Unidad", null, 55m, null, c1.Id, 5m);
        fixture.Uow.Productos.Agregar(p1);
        fixture.Uow.Productos.Agregar(p2);
        await fixture.Uow.GuardarAsync();

        var lote = await fixture.Uow.Productos.ObtenerPorIdsAsync([p1.Id, p2.Id]);
        Assert.Equal(2, lote.Count);
        Assert.Equal("Olla", lote[p1.Id].Nombre);
        Assert.Equal("Sartén", lote[p2.Id].Nombre);

        var inv1 = new Inventario { ProductoId = p1.Id, AlmacenId = Almacen.PrincipalId, Cantidad = 12m };
        fixture.Uow.Inventarios.Agregar(inv1);
        await fixture.Uow.GuardarAsync();

        var invLote = await fixture.Uow.Inventarios.ObtenerPorProductosAsync([p1.Id, p2.Id], Almacen.PrincipalId);
        Assert.Single(invLote);
        Assert.Equal(12m, invLote[p1.Id].Cantidad);
    }

    [Fact]
    public async Task UnitOfWork_EjecutarEnTransaccion_RevierteAnteFallo()
    {
        using var fixture = new ApplicationFixture();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Uow.EjecutarEnTransaccionAsync<int>(async ct =>
            {
                fixture.Uow.Categorias.Agregar(new Categoria("Temporal"));
                await fixture.Uow.GuardarAsync(ct);
                throw new InvalidOperationException("Fallo simulado");
            }));

        var lista = await fixture.Uow.Categorias.ListarAsync();
        Assert.Empty(lista);
    }
}
