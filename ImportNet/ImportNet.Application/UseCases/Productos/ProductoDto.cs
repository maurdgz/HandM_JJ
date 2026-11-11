using System.Text.Json.Serialization;
using ImportNet.Domain.Entities;

namespace ImportNet.Application.UseCases.Productos;

public sealed record ProductoDto(int Id, string Codigo, string Nombre, string? Descripcion, string UnidadMedida,
    decimal? Peso, decimal PrecioVenta, string? PartidaArancelaria, int CategoriaId, decimal StockMinimo, bool Activo,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? CostoPromedio,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? CostoImportacionUsd,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? TipoCambioImportacion)
{
    public static ProductoDto Desde(Producto p, bool verCostos) => new(p.Id, p.Codigo, p.Nombre, p.Descripcion,
        p.UnidadMedida, p.Peso, p.PrecioVenta, p.PartidaArancelaria, p.CategoriaId, p.StockMinimo, p.Activo,
        verCostos ? p.CostoPromedio : null, verCostos ? p.CostoImportacionUsd : null, verCostos ? p.TipoCambioImportacion : null);
}
