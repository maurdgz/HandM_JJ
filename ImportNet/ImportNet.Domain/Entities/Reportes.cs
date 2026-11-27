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
