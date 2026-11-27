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
