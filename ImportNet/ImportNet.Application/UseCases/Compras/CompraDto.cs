using ImportNet.Domain.Entities;
namespace ImportNet.Application.UseCases.Compras;
public sealed record CompraDto(int Id, int ProveedorId, DateTime Fecha, string Estado, decimal TipoCambio, decimal FletePen, decimal AduanaPen, decimal SubtotalUsd, decimal MercaderiaPen, decimal TotalPen, IReadOnlyList<DetalleCompraDto> Lineas)
{
    public static CompraDto Desde(Compra c) => new(c.Id, c.ProveedorId, c.Fecha, c.Estado.ToString(), c.TipoCambio, c.FletePen, c.AduanaPen, c.SubtotalUsd, c.MercaderiaPen, c.TotalPen, c.Lineas.OrderBy(x => x.Orden).Select(x => new DetalleCompraDto(x.ProductoId, x.Cantidad, x.CostoUnitarioUsd, x.SubtotalUsd, x.MercaderiaPen, x.FletePen, x.AduanaPen, x.TotalPen, x.CostoUnitarioPuestoPen)).ToArray());
}
public sealed record DetalleCompraDto(int ProductoId, decimal Cantidad, decimal CostoUnitarioUsd, decimal SubtotalUsd, decimal MercaderiaPen, decimal FletePen, decimal AduanaPen, decimal TotalPen, decimal CostoUnitarioPuestoPen);
