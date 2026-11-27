using ImportNet.Domain.Common;
using ImportNet.Domain.Exceptions;

namespace ImportNet.Domain.Entities;

public sealed class DetalleVenta : Entity
{
    public int VentaId { get; set; }
    public int ProductoId { get; set; }
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Descuento { get; set; }
    public decimal Subtotal { get; set; }

    public Venta Venta { get; set; } = null!;
    public Producto Producto { get; set; } = null!;

    private DetalleVenta()
    {
    }

    public DetalleVenta(int productoId, decimal cantidad, decimal precioUnitario, decimal descuento)
    {
        if (productoId <= 0 || cantidad <= 0 || cantidad > 1000000 || decimal.Round(cantidad, 3) != cantidad)
            throw new ReglaNegocioException("Cantidad, precio o descuento de línea inválidos (descuento máximo 50%).");

        if (precioUnitario < 0 || decimal.Round(precioUnitario, 2) != precioUnitario)
            throw new ReglaNegocioException("Cantidad, precio o descuento de línea inválidos (descuento máximo 50%).");

        if (descuento < 0 || descuento > precioUnitario * cantidad * 0.5m || decimal.Round(descuento, 2) != descuento)
            throw new ReglaNegocioException("Cantidad, precio o descuento de línea inválidos (descuento máximo 50%).");

        ProductoId = productoId;
        Cantidad = cantidad;
        PrecioUnitario = precioUnitario;
        Descuento = descuento;
        Subtotal = decimal.Round(cantidad * precioUnitario, 2, MidpointRounding.AwayFromZero) - descuento;
    }
}
