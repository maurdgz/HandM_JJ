namespace ImportNet.Domain.Entities;

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
