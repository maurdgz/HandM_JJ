using ImportNet.Domain.Entities;
using Xunit;

namespace ImportNet.Tests;

public sealed class RecepcionDominioTests
{
    private static Compra NuevaCompra() =>
        new(
            1,
            new DateTime(2026, 11, 20, 0, 0, 0, DateTimeKind.Utc),
            3.8m,
            10m,
            5m,
            [new LineaCompra(1, 2m, 10m)]);

    [Fact]
    public void Recibir_CambiaEstadoYConservaCostos()
    {
        var compra = NuevaCompra();
        var totalAnterior = compra.TotalPen;

        compra.Recibir();

        Assert.Equal(EstadoCompra.Recibida, compra.Estado);
        Assert.Equal(totalAnterior, compra.TotalPen);
        Assert.Single(compra.Lineas);
        Assert.Equal(2m, compra.Lineas[0].Cantidad);
    }

    [Fact]
    public void Recibida_RechazaSegundaRecepcionYEdicion()
    {
        var compra = NuevaCompra();
        compra.Recibir();
        var totalAnterior = compra.TotalPen;

        Assert.Throws<ConflictoException>(() => compra.Recibir());

        Assert.Throws<ConflictoException>(() => compra.Editar(
            1,
            DateTime.UtcNow,
            4m,
            0m,
            0m,
            [new LineaCompra(1, 10m, 20m)]));

        Assert.Equal(EstadoCompra.Recibida, compra.Estado);
        Assert.Equal(totalAnterior, compra.TotalPen);
        Assert.Equal(2m, compra.Lineas[0].Cantidad);
    }

    [Fact]
    public void CostoPromedio_CombinaExistenciasConEntrada()
    {
        var resultado = CalculadoraCostoPromedio.Calcular(
            stockActual: 10m,
            costoPromedioActual: 20m,
            cantidadEntrada: 5m,
            costoTotalEntradaPen: 150m);

        Assert.Equal(23.3333m, resultado);
    }

    [Fact]
    public void SinStock_UtilizaTotalPersistidoDeLaLinea()
    {
        var compra = new Compra(
            1,
            DateTime.UtcNow,
            1m,
            0.01m,
            0m,
            [new LineaCompra(1, 3m, 1m)]);

        var linea = compra.Lineas[0];

        var resultado = CalculadoraCostoPromedio.Calcular(
            0m, 0m, linea.Cantidad, linea.TotalPen);

        Assert.Equal(3.01m, linea.TotalPen);
        Assert.Equal(1.0033m, resultado);
    }

    [Fact]
    public void CostoPromedio_RedondeaCuatroDecimalesAwayFromZero()
    {
        var resultado = CalculadoraCostoPromedio.Calcular(
            1m, 0m, 199m, 0.01m);

        Assert.Equal(0.0001m, resultado);
    }

    [Theory]
    [InlineData(-1, 20, 5, 150)]
    [InlineData(10, -1, 5, 150)]
    [InlineData(10, 20, 0, 150)]
    [InlineData(10, 20, -1, 150)]
    [InlineData(10, 20, 5, -1)]
    public void CostoPromedio_RechazaValoresInvalidos(
        decimal stock,
        decimal costo,
        decimal cantidad,
        decimal total)
    {
        Assert.Throws<ReglaNegocioException>(() =>
            CalculadoraCostoPromedio.Calcular(
                stock, costo, cantidad, total));
    }
}