using ImportNet.Application.UseCases.Inventario;
using ImportNet.Domain.Entities;
using ImportNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ImportNet.Tests;

public sealed class StockConsultaTests
{
    [Fact]
    public async Task Consulta_IncluyeProductosSinInventario()
    {
        using var fixture = new ApplicationFixture();
        var datos = await Preparar(fixture.Db);

        var resultado = await fixture.Mediator.Send(
            new ConsultarStockQuery());

        Assert.Equal(3, resultado.Count);
        Assert.Equal(3, resultado.Items.Count);

        var caja = resultado.Items.Single(
            x => x.ProductoId == datos.CajaId);

        var olla = resultado.Items.Single(
            x => x.ProductoId == datos.OllaId);

        Assert.Equal(0m, caja.Cantidad);
        Assert.True(caja.StockBajo);
        Assert.Equal(7.5m, olla.Cantidad);

        Assert.All(
            resultado.Items,
            item => Assert.Equal(
                Almacen.PrincipalId,
                item.AlmacenId));
    }

    [Fact]
    public async Task StockBajo_IncluyeCantidadIgualAlMinimo()
    {
        using var fixture = new ApplicationFixture();
        var datos = await Preparar(fixture.Db);

        var resultado = await fixture.Mediator.Send(
            new ConsultarStockQuery(StockBajo: true));

        Assert.Equal(2, resultado.Count);

        Assert.Equal(
            new[] { datos.CajaId, datos.SartenId },
            resultado.Items
                .Select(x => x.ProductoId)
                .ToArray());

        Assert.All(
            resultado.Items,
            item => Assert.True(item.StockBajo));

        var sarten = resultado.Items.Single(
            x => x.ProductoId == datos.SartenId);

        Assert.Equal(
            sarten.StockMinimo,
            sarten.Cantidad);
    }

    [Fact]
    public async Task Busqueda_AdmiteNombreCodigoYEspacios()
    {
        using var fixture = new ApplicationFixture();
        var datos = await Preparar(fixture.Db);

        var porNombre = await fixture.Mediator.Send(
            new ConsultarStockQuery(
                Texto: "  oLLa  "));

        Assert.Equal(1, porNombre.Count);

        Assert.Equal(
            datos.OllaId,
            Assert.Single(porNombre.Items).ProductoId);

        var porCodigo = await fixture.Mediator.Send(
            new ConsultarStockQuery(
                Texto: "  sar-02  "));

        Assert.Equal(1, porCodigo.Count);

        Assert.Equal(
            datos.SartenId,
            Assert.Single(porCodigo.Items).ProductoId);
    }

    [Fact]
    public async Task Paginacion_ConservaTotalYOrden()
    {
        using var fixture = new ApplicationFixture();
        var datos = await Preparar(fixture.Db);

        var primera = await fixture.Mediator.Send(
            new ConsultarStockQuery(
                Pagina: 1,
                Tamano: 2));

        var segunda = await fixture.Mediator.Send(
            new ConsultarStockQuery(
                Pagina: 2,
                Tamano: 2));

        Assert.Equal(3, primera.Count);
        Assert.Equal(3, segunda.Count);

        Assert.Equal(
            new[] { datos.CajaId, datos.OllaId },
            primera.Items
                .Select(x => x.ProductoId)
                .ToArray());

        Assert.Equal(
            datos.SartenId,
            Assert.Single(segunda.Items).ProductoId);

        Assert.Equal(2, segunda.PaginaActual);
        Assert.Equal(2, segunda.Tamano);
    }

    [Fact]
    public async Task ProductoInactivo_ConservaStockVisible()
    {
        using var fixture = new ApplicationFixture();
        var datos = await Preparar(fixture.Db);

        var producto = await fixture.Db.Set<Producto>()
            .SingleAsync(
                x => x.Id == datos.SartenId);

        fixture.Db.Entry(producto)
            .Property(x => x.Activo)
            .CurrentValue = false;

        await fixture.Db.SaveChangesAsync();
        fixture.Db.ChangeTracker.Clear();

        var resultado = await fixture.Mediator.Send(
            new ConsultarStockQuery());

        Assert.Equal(3, resultado.Count);

        var sarten = resultado.Items.Single(
            x => x.ProductoId == datos.SartenId);

        Assert.False(sarten.Activo);
        Assert.Equal(2m, sarten.Cantidad);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    [InlineData(1000001, 20)]
    public async Task PaginacionInvalida_SeRechaza(
        int pagina,
        int tamano)
    {
        using var fixture = new ApplicationFixture();

        await Assert.ThrowsAsync<ReglaNegocioException>(
            () => fixture.Mediator.Send(
                new ConsultarStockQuery(
                    Pagina: pagina,
                    Tamano: tamano)));
    }

    [Fact]
    public async Task BusquedaDemasiadoLarga_SeRechaza()
    {
        using var fixture = new ApplicationFixture();

        await Assert.ThrowsAsync<ReglaNegocioException>(
            () => fixture.Mediator.Send(
                new ConsultarStockQuery(
                    Texto: new string('a', 101))));
    }

    internal sealed record DatosStock(
        int OllaId,
        int SartenId,
        int CajaId);

    internal static async Task<DatosStock> Preparar(
        AppDbContext db)
    {
        var categoria = new Categoria(
            "Productos de prueba");

        db.Add(categoria);
        await db.SaveChangesAsync();

        var olla = new Producto(
            "OL-01",
            "Olla",
            null,
            "Unidad",
            null,
            80m,
            null,
            categoria.Id,
            5m);

        var sarten = new Producto(
            "SAR-02",
            "Sarten",
            null,
            "Unidad",
            null,
            60m,
            null,
            categoria.Id,
            2m);

        var caja = new Producto(
            "CAJ-03",
            "Caja organizadora",
            null,
            "Unidad",
            null,
            25m,
            null,
            categoria.Id,
            1m);

        olla.ActualizarCostoPromedioPorRecepcion(
            0m,
            7.5m,
            150m);

        sarten.ActualizarCostoPromedioPorRecepcion(
            0m,
            2m,
            60m);

        db.AddRange(olla, sarten, caja);
        await db.SaveChangesAsync();

        db.AddRange(
            new Inventario
            {
                ProductoId = olla.Id,
                AlmacenId = Almacen.PrincipalId,
                Cantidad = 7.5m
            },
            new Inventario
            {
                ProductoId = sarten.Id,
                AlmacenId = Almacen.PrincipalId,
                Cantidad = 2m
            });

        await db.SaveChangesAsync();

        var resultado = new DatosStock(
            olla.Id,
            sarten.Id,
            caja.Id);

        db.ChangeTracker.Clear();

        return resultado;
    }
}