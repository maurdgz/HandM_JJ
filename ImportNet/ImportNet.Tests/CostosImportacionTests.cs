using ImportNet.Domain.Entities;
using Xunit;
namespace ImportNet.Tests;
public sealed class CostosImportacionTests
{
    [Fact] public void Proporcional_ReparteGastosPorValorUsd()
    {
        var c = CalculadoraCostoImportacion.Calcular([new(1, 2, 10), new(2, 1, 60)], 3.75m, 40, 20);
        Assert.Equal(80, c.SubtotalUsd); Assert.Equal(300, c.MercaderiaPen); Assert.Equal(360, c.TotalPen);
        Assert.Equal(new CostoLineaImportacion(75, 10, 5), c.Lineas[0]);
        Assert.Equal(new CostoLineaImportacion(225, 30, 15), c.Lineas[1]);
    }
    [Fact] public void TresLineas_ReparteCentavoResidualYConversion()
    {
        LineaCompra[] lineas = [new(1, 1, 1), new(2, 1, 1), new(3, 1, 1)];
        var c = CalculadoraCostoImportacion.Calcular(lineas, 3.333333m, .01m, .02m);
        Assert.Equal(10m, c.MercaderiaPen); Assert.Equal(10.03m, c.TotalPen);
        Assert.Equal(new CostoLineaImportacion(3.34m, .01m, .01m), c.Lineas[0]);
        Assert.Equal(new CostoLineaImportacion(3.33m, 0, .01m), c.Lineas[1]);
        Assert.Equal(new CostoLineaImportacion(3.33m, 0, 0), c.Lineas[2]);
        Assert.Equal(c.TotalPen, c.Lineas.Sum(x => x.TotalPen));
        Assert.Equal(c, c with { });
        Assert.Equal(c.Lineas, CalculadoraCostoImportacion.Calcular(lineas, 3.333333m, .01m, .02m).Lineas);
    }
    [Fact] public void CantidadFraccionaria_NoReconstruyeTotalConUnitarioRedondeado()
    {
        var c = new Compra(1, DateTime.UtcNow, 1, .01m, 0, [new(1, 3, 1)]);
        Assert.Equal(3.01m, c.TotalPen);
        Assert.NotEqual(c.TotalPen, decimal.Round(c.Lineas[0].CostoUnitarioPuestoPen, 2) * 3);
    }
    [Theory] [InlineData(0, 1, 1)] [InlineData(-1, 1, 1)] [InlineData(3.8, 0, 1)] [InlineData(3.8, -1, 1)] [InlineData(3.8, 1, 0)]
    public void RechazaBaseInvalida(decimal cambio, decimal cantidad, decimal costo)
        => Assert.Throws<ReglaNegocioException>(() => new Compra(1, DateTime.UtcNow, cambio, 0, 0, [new(1, cantidad, costo)]));
    [Fact] public void RechazaSinLineasGastosNegativosYNoMutaSiFalla()
    {
        Assert.Throws<ReglaNegocioException>(() => new Compra(1, DateTime.UtcNow, 3.8m, 0, 0, []));
        var c = new Compra(1, DateTime.UtcNow, 3.8m, 0, 0, [new(1, 1, 1)]);
        Assert.Throws<ReglaNegocioException>(() => c.Editar(1, DateTime.UtcNow, 3.8m, -1, 0, [new(1, 1, 1)]));
        Assert.Equal(3.8m, c.TotalPen);
    }
    [Fact] public void MuchosRepartos_CierranExactamente()
    {
        for (int n = 1; n <= 100; n++)
        {
            var lineas = Enumerable.Range(1, n).Select(i => new LineaCompra(i, i / 3m, i * .1234m)).ToArray();
            var c = CalculadoraCostoImportacion.Calcular(lineas, 3.712345m, 17.11m, 23.33m);
            Assert.Equal(c.TotalPen, c.Lineas.Sum(x => x.TotalPen));
            Assert.Equal(c.MercaderiaPen, c.Lineas.Sum(x => x.MercaderiaPen));
            Assert.Equal(17.11m, c.Lineas.Sum(x => x.FletePen)); Assert.Equal(23.33m, c.Lineas.Sum(x => x.AduanaPen));
        }
    }
}
