namespace ImportNet.Domain.Entities;

public sealed record LineaKardex(
    int MovimientoId,
    DateTime Fecha,
    string Tipo,
    string Referencia,
    int? CompraId,
    int? VentaId,
    decimal Entrada,
    decimal Salida,
    decimal Saldo,
    string Empleado,
    string? Motivo,
    string? Observacion);

public sealed record KardexProducto(
    int ProductoId,
    string Codigo,
    string Nombre,
    string UnidadMedida,
    decimal SaldoInicial,
    decimal TotalEntradas,
    decimal TotalSalidas,
    decimal SaldoFinal,
    decimal StockActual,
    IReadOnlyList<LineaKardex> Movimientos);
