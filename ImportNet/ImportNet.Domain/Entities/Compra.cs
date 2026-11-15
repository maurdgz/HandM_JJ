namespace ImportNet.Domain.Entities;
public enum EstadoCompra { Borrador, Recibida }
public sealed class Compra
{
    public int Id { get; private set; }
    public int ProveedorId { get; private set; }
    public DateTime Fecha { get; private set; }
    public decimal TipoCambio { get; private set; }
    public decimal FletePen { get; private set; }
    public decimal AduanaPen { get; private set; }
    public EstadoCompra Estado { get; private set; } = EstadoCompra.Borrador;
    public decimal SubtotalUsd => Lineas.Sum(x => x.SubtotalUsd);
    public decimal MercaderiaPen => Lineas.Sum(x => x.MercaderiaPen);
    public decimal TotalPen => Lineas.Sum(x => x.TotalPen);
    public uint Version { get; private set; }
    private readonly List<DetalleCompra> _lineas = [];
    public IReadOnlyList<DetalleCompra> Lineas => _lineas.AsReadOnly();
    private Compra() { }
    public Compra(int proveedorId, DateTime fecha, decimal tipoCambio, decimal fletePen, decimal aduanaPen, IReadOnlyList<LineaCompra> lineas)
        => Editar(proveedorId, fecha, tipoCambio, fletePen, aduanaPen, lineas);
    public void Editar(int proveedorId, DateTime fecha, decimal tipoCambio, decimal fletePen, decimal aduanaPen, IReadOnlyList<LineaCompra> lineas)
    {
        ExigirBorrador();
        if (proveedorId <= 0 || fecha == default || fecha.Kind != DateTimeKind.Utc) throw new ReglaNegocioException("Proveedor y fecha UTC validos requeridos.");
        if (tipoCambio <= 0 || tipoCambio > 1000 || decimal.Round(tipoCambio, 6) != tipoCambio) throw new ReglaNegocioException("Tipo de cambio USD a PEN positivo, hasta 1000 y 6 decimales.");
        ValidarGasto(fletePen); ValidarGasto(aduanaPen);
        if (lineas is null || lineas.Count == 0 || lineas.Count > 100 || lineas.Any(x => x is null)) throw new ReglaNegocioException("Indique de 1 a 100 lineas.");
        if (lineas.Select(x => x.ProductoId).Distinct().Count() != lineas.Count) throw new ReglaNegocioException("No repita productos en una compra.");
        var nuevas = lineas.Select((x, i) => new DetalleCompra(i + 1, x.ProductoId, x.Cantidad, x.CostoUnitarioUsd)).ToList();
        var costos = CalculadoraCostoImportacion.Calcular(lineas, tipoCambio, fletePen, aduanaPen);
        for (var i = 0; i < nuevas.Count; i++) nuevas[i].AsignarCostos(costos.Lineas[i]);
        ProveedorId = proveedorId; Fecha = fecha; TipoCambio = tipoCambio; FletePen = fletePen; AduanaPen = aduanaPen;
        _lineas.Clear(); _lineas.AddRange(nuevas);
    }
    public void ExigirBorrador()
    {
        if (Estado != EstadoCompra.Borrador) throw new ConflictoException("Una compra recibida no se puede editar ni eliminar.");
    }
    private static void ValidarGasto(decimal importe)
    {
        if (importe < 0 || importe > 1000000000000m || decimal.Round(importe, 2) != importe) throw new ReglaNegocioException("Gastos PEN no negativos, hasta 1 billon y 2 decimales.");
    }
}
public sealed record LineaCompra(int ProductoId, decimal Cantidad, decimal CostoUnitarioUsd);
public sealed class DetalleCompra
{
    public int Id { get; private set; }
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
    internal void AsignarCostos(CostoLineaImportacion costo)
    { MercaderiaPen = costo.MercaderiaPen; FletePen = costo.FletePen; AduanaPen = costo.AduanaPen; }
    public decimal SubtotalUsd => Cantidad * CostoUnitarioUsd;
    private DetalleCompra() { }
    internal DetalleCompra(int orden, int productoId, decimal cantidad, decimal costoUnitarioUsd)
    {
        if (productoId <= 0 || cantidad <= 0 || cantidad > 1000000 || decimal.Round(cantidad, 3) != cantidad || costoUnitarioUsd <= 0 || costoUnitarioUsd > 1000000 || decimal.Round(costoUnitarioUsd, 4) != costoUnitarioUsd)
            throw new ReglaNegocioException("Producto valido, cantidad positiva (3 decimales) y costo USD positivo (4 decimales), hasta un millon.");
        Orden = orden; ProductoId = productoId; Cantidad = cantidad; CostoUnitarioUsd = costoUnitarioUsd; }
}
