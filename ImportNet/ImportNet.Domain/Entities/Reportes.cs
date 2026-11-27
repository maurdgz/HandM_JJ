namespace ImportNet.Domain.Entities;

public enum AgrupacionPeriodo { Dia, Mes }

public sealed record VentasPorPeriodo(string Periodo, int Ventas, decimal Total);

public sealed record VentasPorVendedor(int EmpleadoId, string Vendedor, int Ventas, decimal Total, decimal TicketPromedio);

public sealed record TopProducto(int ProductoId, string Codigo, string Nombre, string UnidadMedida, decimal Cantidad, decimal Total);

public sealed record MargenProducto(int ProductoId, string Codigo, string Nombre, decimal Cantidad, decimal Ingreso,
    decimal CostoPromedio, decimal Costo, decimal Margen, decimal MargenPorcentaje);

public sealed record ProductoStockBajo(int ProductoId, string Codigo, string Nombre, string UnidadMedida, decimal Stock,
    decimal StockMinimo, decimal Faltante);

public sealed record CierreVendedor(int EmpleadoId, string Vendedor, int Ventas, decimal Total);

public sealed record CierreCaja(DateOnly Fecha, int Ventas, decimal Subtotal, decimal Descuento, decimal Total, int Anuladas,
    decimal MontoAnulado, IReadOnlyList<CierreVendedor> PorVendedor);

public sealed record ComprasPorProveedor(int ProveedorId, string Proveedor, string Pais, int Compras, decimal TotalUsd, decimal TotalPen);
