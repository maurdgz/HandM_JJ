using ImportNet.Domain.Common;

namespace ImportNet.Domain.Entities;

public sealed class MovimientoInventario : Entity
{
    public int ProductoId { get; set; }
    public int AlmacenId { get; set; }
    public TipoMovimiento Tipo { get; set; }
    public decimal Cantidad { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public int EmpleadoId { get; set; }
    public int? ProveedorId { get; set; }
    public int? CompraId { get; set; }
    public int? VentaId { get; set; }
    public MotivoAjuste? MotivoAjuste { get; set; }
    public string? Observacion { get; set; }

    public Empleado Empleado { get; set; } = null!;
    public Venta? Venta { get; set; }
}
