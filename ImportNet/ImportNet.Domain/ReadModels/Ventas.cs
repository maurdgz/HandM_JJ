namespace ImportNet.Domain.ReadModels;

public sealed record VentaResumen(
    int Id,
    int Numero,
    DateTime Fecha,
    int EmpleadoId,
    string Vendedor,
    string Cliente,
    string? DocumentoCliente,
    string Estado,
    decimal Subtotal,
    decimal Descuento,
    decimal Total,
    string? MotivoAnulacion);

public sealed record VentaLinea(
    string Codigo,
    string Producto,
    string UnidadMedida,
    decimal Cantidad,
    decimal PrecioUnitario,
    decimal Descuento,
    decimal Subtotal);

public sealed record NotaVenta(
    VentaResumen Venta,
    IReadOnlyList<VentaLinea> Lineas);