using ImportNet.Domain.Entities;
using ImportNet.Domain.Exceptions;
using Xunit;

namespace ImportNet.Tests;

public sealed class DominioReglasTests
{
    [Fact]
    public void Categoria_NormalizaNombreYRechazaVacio()
    {
        var categoria = new Categoria("  Electrónica  ");
        Assert.Equal("Electrónica", categoria.Nombre);
        Assert.Equal("ELECTRÓNICA", categoria.NombreNormalizado);

        Assert.Throws<ReglaNegocioException>(() => new Categoria("   "));
    }

    [Fact]
    public void Producto_ValidaImportacionConjuntaYCostoPromedio()
    {
        var producto = new Producto("usb-01", "Cable USB", null, "Unidad", 0.125m, 15.50m, null, 1, 5m);
        Assert.Equal("USB-01", producto.Codigo);

        Assert.Throws<ReglaNegocioException>(() => producto.RegistrarCostoImportacion(10m, null));

        producto.RegistrarCostoImportacion(4.50m, 3.75m);
        Assert.Equal(4.50m, producto.CostoImportacionUsd);

        producto.ActualizarCostoPromedioPorRecepcion(10m, 10m, 100m);
        Assert.Equal(5.0000m, producto.CostoPromedio);
    }

    [Fact]
    public void CalculadoraCostoImportacion_ProrrateaCentavosExactos()
    {
        var lineas = new List<LineaCompra>
        {
            new(1, 2m, 10m),
            new(2, 1m, 10m)
        };

        var costos = CalculadoraCostoImportacion.Calcular(lineas, 3.50m, 10m, 5m);
        Assert.Equal(2, costos.Count);
        Assert.Equal(105.00m, costos.Sum(c => c.MercaderiaPen));
        Assert.Equal(10.00m, costos.Sum(c => c.FletePen));
        Assert.Equal(5.00m, costos.Sum(c => c.AduanaPen));
    }

    [Fact]
    public void Compra_RecibidaEsInmutable()
    {
        var compra = new Compra(1, DateTime.UtcNow, 3.75m, 10m, 5m, [new LineaCompra(1, 2m, 10m)]);
        compra.Recibir();

        Assert.Equal(EstadoCompra.Recibida, compra.Estado);
        Assert.Throws<ConflictoException>(() => compra.Recibir());
        Assert.Throws<ConflictoException>(() => compra.ExigirBorrador());
    }

    [Fact]
    public void VentaYDetalle_ValidanDescuentoMaximoYAnulacionUnica()
    {
        Assert.Throws<ReglaNegocioException>(() => new DetalleVenta(1, 2m, 10m, 10.01m));

        var venta = new Venta(1, 1, 1, 100m, 10m, DateTime.UtcNow);
        Assert.Equal(90m, venta.Total);

        venta.Anular("Error de digitación");
        Assert.Equal(EstadoVenta.Anulada, venta.Estado);
        Assert.Throws<ReglaNegocioException>(() => venta.Anular("Otro motivo"));
    }
}
