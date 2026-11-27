namespace ImportNet.Domain.Entities;

public enum AgrupacionPeriodo { Dia, Mes }

/// <summary>Periodo en formato AAAA-MM-DD (día) o AAAA-MM (mes), en hora de Perú.</summary>
public sealed record VentasPorPeriodo(
    string Periodo,
    int Ventas,
    decimal Total);

public sealed record VentasPorVendedor(
    int EmpleadoId,
    string Vendedor,
    int Ventas,
    decimal Total,
    decimal TicketPromedio);

public sealed record TopProducto(
    int ProductoId,
    string Codigo,
    string Nombre,
    string UnidadMedida,
    decimal Cantidad,
    decimal Total);

/// <summary>
/// Margen = ingreso neto (con descuentos) − cantidad × costo promedio actual del producto.
/// La venta no guarda el costo del momento, así que se usa el costo promedio vigente.
/// </summary>
public sealed record MargenProducto(
    int ProductoId,
    string Codigo,
    string Nombre,
    decimal Cantidad,
    decimal Ingreso,
    decimal CostoPromedio,
    decimal Costo,
    decimal Margen,
    decimal MargenPorcentaje);

public sealed record ProductoStockBajo(
    int ProductoId,
    string Codigo,
    string Nombre,
    string UnidadMedida,
    decimal Stock,
    decimal StockMinimo,
    decimal Faltante);

public sealed record CierreVendedor(
    int EmpleadoId,
    string Vendedor,
    int Ventas,
    decimal Total);

/// <summary>
/// Cierre de un día (hora de Perú). Los importes solo suman ventas Registradas; las anuladas
/// de ese día se informan aparte y no entran en el total.
/// </summary>
public sealed record CierreCaja(
    DateOnly Fecha,
    int Ventas,
    decimal Subtotal,
    decimal Descuento,
    decimal Total,
    int Anuladas,
    decimal MontoAnulado,
    IReadOnlyList<CierreVendedor> PorVendedor);

/// <summary>Solo compras Recibidas.</summary>
public sealed record ComprasPorProveedor(
    int ProveedorId,
    string Proveedor,
    string Pais,
    int Compras,
    decimal TotalUsd,
    decimal TotalPen);
