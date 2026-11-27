using System.Text.Json.Serialization;
using ImportNet.Domain.Entities;

namespace ImportNet.Application.UseCases.Productos;

public sealed class ProductoDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public string UnidadMedida { get; set; } = "";
    public decimal? Peso { get; set; }
    public decimal PrecioVenta { get; set; }
    public string? PartidaArancelaria { get; set; }
    public int CategoriaId { get; set; }
    public decimal StockMinimo { get; set; }
    public bool Activo { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? CostoPromedio { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? CostoImportacionUsd { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? TipoCambioImportacion { get; set; }

    public static ProductoDto Desde(Producto p, bool verCostos)
    {
        var dto = new ProductoDto
        {
            Id = p.Id,
            Codigo = p.Codigo,
            Nombre = p.Nombre,
            Descripcion = p.Descripcion,
            UnidadMedida = p.UnidadMedida,
            Peso = p.Peso,
            PrecioVenta = p.PrecioVenta,
            PartidaArancelaria = p.PartidaArancelaria,
            CategoriaId = p.CategoriaId,
            StockMinimo = p.StockMinimo,
            Activo = p.Activo
        };

        if (verCostos)
        {
            dto.CostoPromedio = p.CostoPromedio;
            dto.CostoImportacionUsd = p.CostoImportacionUsd;
            dto.TipoCambioImportacion = p.TipoCambioImportacion;
        }

        return dto;
    }
}
