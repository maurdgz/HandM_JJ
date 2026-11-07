namespace ImportNet.Domain.Entities;

// Operational aggregates are mapped for the next iteration; no stock or sales write API is exposed yet.
public sealed class Empleado
{
    public int Id { get; set; }
    public string UsuarioId { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Cargo { get; set; } = "";
    public string Area { get; set; } = "";
    public bool Activo { get; set; } = true;
}
public sealed class Almacen
{
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
    public int? OrdenVentaId { get; set; }
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
public sealed class Cliente
{
    public int Id { get; set; }
    public string Empresa { get; set; } = "";
    public string Pais { get; set; } = "";
    public string Contacto { get; set; } = "";
    public string MonedaPreferida { get; set; } = "USD";
    public string IncotermPreferido { get; set; } = "";
}
public enum EstadoOrden { Pendiente, Despachado, Enviado, Entregado, Anulado }
public sealed class OrdenVenta
{
    public int Id { get; set; }
    public int EmpleadoId { get; set; }
    public int ClienteId { get; set; }
    public int AlmacenId { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public EstadoOrden Estado { get; private set; }
    public string Moneda { get; set; } = "USD";
    public decimal TipoCambio { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
    public string Incoterm { get; set; } = "";
    public string PaisDestino { get; set; } = "";
    public uint Version { get; set; }

    public void CambiarEstado(EstadoOrden destino)
    {
        var valido = (Estado, destino) switch
        {
            (EstadoOrden.Pendiente, EstadoOrden.Despachado or EstadoOrden.Anulado) => true,
            (EstadoOrden.Despachado, EstadoOrden.Enviado or EstadoOrden.Anulado) => true,
            (EstadoOrden.Enviado, EstadoOrden.Entregado) => true,
            _ => false
        };
        if (!valido) throw new ReglaNegocioException("Transición de estado no permitida.");
        Estado = destino;
    }
}
public sealed class DetalleOrdenVenta
{
    public int Id { get; set; }
    public int OrdenVentaId { get; set; }
    public int ProductoId { get; set; }
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}
