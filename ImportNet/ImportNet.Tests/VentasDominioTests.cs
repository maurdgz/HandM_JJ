using ImportNet.Domain.Entities;
using Xunit;

namespace ImportNet.Tests;

public sealed class VentasDominioTests
{
    [Fact]
    public void Detalle_CalculaImporteEnSolesYAplicaDescuento()
    {
        var detalle = new DetalleVenta(4, 3, 10.25m, 5m);
        Assert.Equal(30.75m, detalle.Subtotal + detalle.Descuento);
        Assert.Equal(25.75m, detalle.Subtotal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Detalle_RechazaCantidadNoPositiva(decimal cantidad)
        => Assert.Throws<ReglaNegocioException>(() => new DetalleVenta(1, cantidad, 2m, 0m));

    [Fact]
    public void Detalle_RechazaDescuentoSuperiorAlLimite()
        => Assert.Throws<ReglaNegocioException>(() => new DetalleVenta(1, 1m, 10m, 5.01m));

    [Fact]
    public void Venta_CalculaTotalEnPen()
    {
        var venta = new Venta(1, 2, 1, 100m, 10m, DateTime.UtcNow);
        Assert.Equal(90m, venta.Total);
    }
}
