namespace ImportNet.Domain.Entities;

public sealed record StockProducto(
    int ProductoId,
    string Codigo,
    string Nombre,
    string UnidadMedida,
    int AlmacenId,
    decimal Cantidad,
    decimal StockMinimo,
    bool Activo)
{
    public bool StockBajo => Cantidad <= StockMinimo;
}