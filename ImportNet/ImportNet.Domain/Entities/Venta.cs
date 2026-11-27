using ImportNet.Domain.Common;
using ImportNet.Domain.Exceptions;

namespace ImportNet.Domain.Entities;

public sealed class Venta : Entity
{
    public int Numero { get; set; }
    public int EmpleadoId { get; set; }
    public int? ClienteId { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public EstadoVenta Estado { get; private set; } = EstadoVenta.Registrada;
    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal Total { get; set; }
    public string? MotivoAnulacion { get; private set; }
    public DateTime? FechaAnulacion { get; private set; }
    public uint Version { get; set; }

    public Empleado Empleado { get; set; } = null!;
    public Cliente? Cliente { get; set; }
    public List<DetalleVenta> Detalles { get; set; } = [];

    public Venta()
    {
    }

    public Venta(int numero, int empleadoId, int clienteId, decimal subtotal, decimal descuento, DateTime fecha)
    {
        if (numero <= 0 || empleadoId <= 0 || clienteId <= 0)
            throw new ReglaNegocioException("Los datos e importes de venta no son válidos.");

        if (subtotal < 0 || descuento < 0 || descuento > subtotal)
            throw new ReglaNegocioException("Los datos e importes de venta no son válidos.");

        Numero = numero;
        EmpleadoId = empleadoId;
        ClienteId = clienteId;
        Subtotal = decimal.Round(subtotal, 2, MidpointRounding.AwayFromZero);
        Descuento = decimal.Round(descuento, 2, MidpointRounding.AwayFromZero);
        Total = Subtotal - Descuento;
        Fecha = fecha;
    }

    public void Anular(string motivo)
    {
        if (Estado == EstadoVenta.Anulada)
            throw new ReglaNegocioException("La venta ya está anulada.");

        MotivoAnulacion = Reglas.Texto(motivo, 300, "Motivo de anulación");
        FechaAnulacion = DateTime.UtcNow;
        Estado = EstadoVenta.Anulada;
    }
}
