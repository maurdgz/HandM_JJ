namespace ImportNet.Domain.Entities;

public sealed class Empleado
{
    public int Id { get; set; }
    public string UsuarioId { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Apellidos { get; set; } = "";
    public string? DocumentoIdentidad { get; set; }
    public string Cargo { get; set; } = "";
    public string Area { get; set; } = "";
    public bool Activo { get; set; } = true;
}
public sealed class Almacen
{
    public const int PrincipalId = 1;
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
}
public sealed class Inventario
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public int AlmacenId { get; set; }
    public decimal Cantidad { get; set; }
    public uint Version { get; set; }
}
public enum TipoMovimiento { Entrada, Salida, Ajuste }
public sealed class MovimientoInventario
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public int AlmacenId { get; set; }
    public TipoMovimiento Tipo { get; set; }
    public decimal Cantidad { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public int EmpleadoId { get; set; }
    public int? ProveedorId { get; set; }
    public int? VentaId { get; set; }
}
public enum TipoProveedor { Nacional, Internacional }
public sealed class Proveedor
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public TipoProveedor Tipo { get; set; }
    public string Pais { get; set; } = "";
    public string Contacto { get; set; } = "";
    public bool Activo { get; set; } = true;
}
public sealed class ProveedorProducto
{
    public int ProveedorId { get; set; }
    public int ProductoId { get; set; }
}
public enum TipoDocumento { Dni, Ruc }
public sealed class Cliente
{
    public int Id { get; set; }
    public TipoDocumento TipoDocumento { get; set; }
    public string NumeroDocumento { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string? Telefono { get; set; }
}
public enum EstadoVenta { Registrada, Anulada }
public sealed class Venta
{
    public int Id { get; set; }
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

    public void Anular(string motivo)
    {
        if (Estado == EstadoVenta.Anulada) throw new ReglaNegocioException("La venta ya está anulada.");
        MotivoAnulacion = Reglas.Texto(motivo, 300, "Motivo de anulación");
        FechaAnulacion = DateTime.UtcNow;
        Estado = EstadoVenta.Anulada;
    }
}
public sealed class DetalleVenta
{
    public int Id { get; set; }
    public int VentaId { get; set; }
    public int ProductoId { get; set; }
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Descuento { get; set; }
    public decimal Subtotal { get; set; }
}
