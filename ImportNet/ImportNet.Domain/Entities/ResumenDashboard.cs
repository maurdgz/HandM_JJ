namespace ImportNet.Domain.Entities;

/// <summary>Importes netos en PEN; fechas comerciales en Perú. Margen al costo promedio vigente.</summary>
public sealed record ResumenDashboard(
    DateOnly Fecha,
    decimal VentasHoy,
    int CantidadVentasHoy,
    decimal VentasMes,
    int CantidadVentasMes,
    decimal TicketPromedioMes,
    decimal MargenMes,
    int ProductosActivos,
    int ProductosBajoMinimo,
    int ProductosSinStock);
