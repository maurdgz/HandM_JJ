using ImportNet.Domain.Common;
using ImportNet.Domain.Exceptions;

namespace ImportNet.Domain.Entities;

public sealed class Compra : Entity
{
    public int ProveedorId { get; private set; }
    public DateTime Fecha { get; private set; }
    public decimal TipoCambio { get; private set; }
    public decimal FletePen { get; private set; }
    public decimal AduanaPen { get; private set; }
    public EstadoCompra Estado { get; private set; } = EstadoCompra.Borrador;
    public uint Version { get; private set; }

    public Proveedor Proveedor { get; private set; } = null!;
    public List<DetalleCompra> Lineas { get; private set; } = [];

    public decimal SubtotalUsd => Lineas.Sum(x => x.SubtotalUsd);
    public decimal MercaderiaPen => Lineas.Sum(x => x.MercaderiaPen);
    public decimal TotalPen => Lineas.Sum(x => x.TotalPen);

    private Compra()
    {
    }

    public Compra(int proveedorId, DateTime fecha, decimal tipoCambio, decimal fletePen, decimal aduanaPen,
        IReadOnlyList<LineaCompra> lineas)
    {
        Editar(proveedorId, fecha, tipoCambio, fletePen, aduanaPen, lineas);
    }

    public void Editar(int proveedorId, DateTime fecha, decimal tipoCambio, decimal fletePen, decimal aduanaPen,
        IReadOnlyList<LineaCompra> lineas)
    {
        ExigirBorrador();

        if (proveedorId <= 0 || fecha == default || fecha.Kind != DateTimeKind.Utc)
            throw new ReglaNegocioException("Proveedor y fecha UTC validos requeridos.");

        if (tipoCambio <= 0 || tipoCambio > 1000 || decimal.Round(tipoCambio, 6) != tipoCambio)
            throw new ReglaNegocioException("Tipo de cambio USD a PEN positivo, hasta 1000 y 6 decimales.");

        ValidarGasto(fletePen);
        ValidarGasto(aduanaPen);

        if (lineas == null || lineas.Count == 0 || lineas.Count > 100 || lineas.Any(x => x == null))
            throw new ReglaNegocioException("Indique de 1 a 100 lineas.");

        if (lineas.Select(x => x.ProductoId).Distinct().Count() != lineas.Count)
            throw new ReglaNegocioException("No repita productos en una compra.");

        var nuevas = new List<DetalleCompra>();
        for (var i = 0; i < lineas.Count; i++)
            nuevas.Add(new DetalleCompra(i + 1, lineas[i].ProductoId, lineas[i].Cantidad, lineas[i].CostoUnitarioUsd));

        var costos = CalculadoraCostoImportacion.Calcular(lineas, tipoCambio, fletePen, aduanaPen);
        for (var i = 0; i < nuevas.Count; i++)
            nuevas[i].AsignarCostos(costos[i]);

        ProveedorId = proveedorId;
        Fecha = fecha;
        TipoCambio = tipoCambio;
        FletePen = fletePen;
        AduanaPen = aduanaPen;

        Lineas.Clear();
        Lineas.AddRange(nuevas);
    }

    public void Recibir()
    {
        if (Estado != EstadoCompra.Borrador)
            throw new ConflictoException("Solo se puede recibir una compra en borrador.");

        Estado = EstadoCompra.Recibida;
    }

    public void ExigirBorrador()
    {
        if (Estado != EstadoCompra.Borrador)
            throw new ConflictoException("Una compra recibida no se puede editar ni eliminar.");
    }

    private static void ValidarGasto(decimal importe)
    {
        if (importe < 0 || importe > 1000000000000m || decimal.Round(importe, 2) != importe)
            throw new ReglaNegocioException("Gastos PEN no negativos, hasta 1 billon y 2 decimales.");
    }
}
