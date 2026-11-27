namespace ImportNet.Domain.Entities;

public sealed record ResumenVenta(
    int Id,
    int Numero,
    DateTime Fecha,
    string Estado,
    int EmpleadoId,
    string Vendedor,
    string Cliente,
    int Lineas,
    decimal Total);

public sealed record LineaNotaVenta(
    int ProductoId,
    string Codigo,
    string Producto,
    string UnidadMedida,
    decimal Cantidad,
    decimal PrecioUnitario,
    decimal Descuento,
    decimal Subtotal);

public sealed record NotaVenta(
    int Id,
    int Numero,
    DateTime Fecha,
    string Estado,
    int EmpleadoId,
    string Vendedor,
    string ClienteTipoDocumento,
    string ClienteDocumento,
    string Cliente,
    decimal Subtotal,
    decimal Descuento,
    decimal Total,
    string? MotivoAnulacion,
    DateTime? FechaAnulacion,
    IReadOnlyList<LineaNotaVenta> Lineas);
