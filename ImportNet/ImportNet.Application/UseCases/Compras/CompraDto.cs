using ImportNet.Domain.Entities;

namespace ImportNet.Application.UseCases.Compras;

public sealed record DetalleCompraDto(int ProductoId, decimal Cantidad, decimal CostoUnitarioUsd, decimal SubtotalUsd, decimal MercaderiaPen,
    decimal FletePen, decimal AduanaPen, decimal TotalPen, decimal CostoUnitarioPuestoPen);

public sealed record CompraDto(int Id, int ProveedorId, DateTime Fecha, string Estado, decimal TipoCambio, decimal FletePen, decimal AduanaPen,
    decimal SubtotalUsd, decimal MercaderiaPen, decimal TotalPen, IReadOnlyList<DetalleCompraDto> Lineas)
{
    public static CompraDto Desde(Compra c)
    {
        var lineas = c.Lineas
            .OrderBy(l => l.Orden)
            .Select(l => new DetalleCompraDto(l.ProductoId, l.Cantidad, l.CostoUnitarioUsd, l.SubtotalUsd, l.MercaderiaPen,
                l.FletePen, l.AduanaPen, l.TotalPen, l.CostoUnitarioPuestoPen))
            .ToList();

        var fecha = DateTime.SpecifyKind(c.Fecha, DateTimeKind.Utc);

        return new CompraDto(c.Id, c.ProveedorId, fecha, c.Estado.ToString(), c.TipoCambio, c.FletePen, c.AduanaPen,
            c.SubtotalUsd, c.MercaderiaPen, c.TotalPen, lineas);
    }
}
