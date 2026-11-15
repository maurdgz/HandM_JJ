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
    public uint Version { get; private set; }
    private readonly List<DetalleCompra> _lineas = [];
    public IReadOnlyList<DetalleCompra> Lineas => _lineas.AsReadOnly();
    private Compra() { }
    public Compra(int proveedorId, DateTime fecha, decimal tipoCambio, decimal fletePen, decimal aduanaPen, IReadOnlyList<LineaCompra> lineas)
        => Editar(proveedorId, fecha, tipoCambio, fletePen, aduanaPen, lineas);
    public void Editar(int proveedorId, DateTime fecha, decimal tipoCambio, decimal fletePen, decimal aduanaPen, IReadOnlyList<LineaCompra> lineas)
    {
        var nuevas = lineas.Select((x, i) => new DetalleCompra(i + 1, x.ProductoId, x.Cantidad, x.CostoUnitarioUsd)).ToList();
        ProveedorId = proveedorId; Fecha = fecha; TipoCambio = tipoCambio; FletePen = fletePen; AduanaPen = aduanaPen;
        _lineas.Clear(); _lineas.AddRange(nuevas);
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
    public decimal SubtotalUsd => Cantidad * CostoUnitarioUsd;
    private DetalleCompra() { }
    internal DetalleCompra(int orden, int productoId, decimal cantidad, decimal costoUnitarioUsd)
    { Orden = orden; ProductoId = productoId; Cantidad = cantidad; CostoUnitarioUsd = costoUnitarioUsd; }
}
