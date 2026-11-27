using ImportNet.Domain.Common;
using ImportNet.Domain.Exceptions;

namespace ImportNet.Domain.Entities;

public sealed class DetalleCompra : Entity
{
    public int CompraId { get; private set; }
    public int Orden { get; private set; }
    public int ProductoId { get; private set; }
    public decimal Cantidad { get; private set; }
    public decimal CostoUnitarioUsd { get; private set; }
    public decimal MercaderiaPen { get; private set; }
    public decimal FletePen { get; private set; }
    public decimal AduanaPen { get; private set; }

    public decimal TotalPen => MercaderiaPen + FletePen + AduanaPen;
    public decimal CostoUnitarioPuestoPen => TotalPen / Cantidad;
    public decimal SubtotalUsd => Cantidad * CostoUnitarioUsd;

    private DetalleCompra()
    {
    }

    internal DetalleCompra(int orden, int productoId, decimal cantidad, decimal costoUnitarioUsd)
    {
        if (productoId <= 0 || cantidad <= 0 || cantidad > 1000000 || decimal.Round(cantidad, 3) != cantidad
            || costoUnitarioUsd <= 0 || costoUnitarioUsd > 1000000 || decimal.Round(costoUnitarioUsd, 4) != costoUnitarioUsd)
            throw new ReglaNegocioException("Producto valido, cantidad positiva (3 decimales) y costo USD positivo (4 decimales), hasta un millon.");

        Orden = orden;
        ProductoId = productoId;
        Cantidad = cantidad;
        CostoUnitarioUsd = costoUnitarioUsd;
    }

    internal void AsignarCostos(CostoLineaImportacion costo)
    {
        MercaderiaPen = costo.MercaderiaPen;
        FletePen = costo.FletePen;
        AduanaPen = costo.AduanaPen;
    }
}
