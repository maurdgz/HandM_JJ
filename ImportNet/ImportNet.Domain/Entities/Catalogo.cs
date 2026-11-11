namespace ImportNet.Domain.Entities;

public abstract class CatalogEntity
{
    public int Id { get; set; }
}

public sealed class ReglaNegocioException(string mensaje) : Exception(mensaje);
public sealed class ConflictoException(string mensaje) : Exception(mensaje);
public sealed class NoEncontradoException(string mensaje) : Exception(mensaje);

public static class Reglas
{
    public static string Texto(string? valor, int max, string campo)
    {
        if (string.IsNullOrWhiteSpace(valor) || valor.Trim().Length > max)
            throw new ReglaNegocioException($"{campo} es obligatorio y admite hasta {max} caracteres.");
        return valor.Trim();
    }
}

public sealed class Categoria : CatalogEntity
{
    public string Nombre { get; private set; } = "";
    public string NombreNormalizado { get; private set; } = "";
    private Categoria() { }
    public Categoria(string nombre) => Editar(nombre);
    public void Editar(string nombre)
    {
        Nombre = Reglas.Texto(nombre, 100, "Nombre");
        NombreNormalizado = Nombre.ToUpperInvariant();
    }
}

public sealed class Producto : CatalogEntity
{
    public string Codigo { get; private set; } = "";
    public string Nombre { get; private set; } = "";
    public string? Descripcion { get; private set; }
    public string UnidadMedida { get; private set; } = "";
    public decimal? Peso { get; private set; }
    public decimal PrecioVenta { get; private set; }
    public decimal CostoPromedio { get; private set; }
    public decimal? CostoImportacionUsd { get; private set; }
    public decimal? TipoCambioImportacion { get; private set; }
    public string? PartidaArancelaria { get; private set; }
    public int CategoriaId { get; private set; }
    public decimal StockMinimo { get; private set; }
    public bool Activo { get; private set; } = true;
    // Reference cost only. Stock receipt and weighted average calculation belong to future purchases.
    public void RegistrarCostoImportacion(decimal? importeUsd, decimal? tipoCambio)
    {
        if (importeUsd.HasValue != tipoCambio.HasValue
            || importeUsd is < 0 or > 9999999999999999.99m
            || tipoCambio is <= 0 or > 999999999999.999999m
            || (importeUsd.HasValue && decimal.Round(importeUsd.Value, 2) != importeUsd)
            || (tipoCambio.HasValue && decimal.Round(tipoCambio.Value, 6) != tipoCambio))
            throw new ReglaNegocioException("Indique costo USD no negativo (2 decimales) y tipo de cambio PEN/USD positivo (6 decimales), o deje ambos vacíos.");
        CostoImportacionUsd = importeUsd;
        TipoCambioImportacion = tipoCambio;
    }
    private Producto() { }
    public Producto(string codigo, string nombre, string? descripcion, string unidadMedida,
        decimal? peso, decimal precioVenta, string? partidaArancelaria, int categoriaId, decimal stockMinimo)
        => Editar(codigo, nombre, descripcion, unidadMedida, peso, precioVenta, partidaArancelaria, categoriaId, stockMinimo);

    public void Editar(string codigo, string nombre, string? descripcion, string unidadMedida,
        decimal? peso, decimal precioVenta, string? partidaArancelaria, int categoriaId, decimal stockMinimo)
    {
        if (peso is not null && (peso <= 0 || peso > 999999999999999.999m || decimal.Round(peso.Value, 3) != peso))
            throw new ReglaNegocioException("Peso debe ser positivo y admitir hasta tres decimales.");
        if (precioVenta < 0 || precioVenta > 9999999999999999.99m || decimal.Round(precioVenta, 2) != precioVenta)
            throw new ReglaNegocioException("Precio de venta debe ser no negativo y admitir hasta dos decimales.");
        if (stockMinimo < 0 || stockMinimo > 999999999999999.999m || decimal.Round(stockMinimo, 3) != stockMinimo || categoriaId <= 0)
            throw new ReglaNegocioException("Categoría o stock mínimo inválidos.");
        if (descripcion?.Length > 1000) throw new ReglaNegocioException("Descripción admite hasta 1000 caracteres.");
        if (partidaArancelaria?.Trim().Length > 20) throw new ReglaNegocioException("Partida arancelaria admite hasta 20 caracteres.");
        var codigoValido = Reglas.Texto(codigo, 40, "Código").ToUpperInvariant();
        var nombreValido = Reglas.Texto(nombre, 200, "Nombre");
        var unidadValida = Reglas.Texto(unidadMedida, 30, "Unidad de medida");
        Codigo = codigoValido; Nombre = nombreValido; UnidadMedida = unidadValida;
        PartidaArancelaria = string.IsNullOrWhiteSpace(partidaArancelaria) ? null : partidaArancelaria.Trim();
        Descripcion = descripcion?.Trim();
        Peso = peso; PrecioVenta = precioVenta; CategoriaId = categoriaId; StockMinimo = stockMinimo;
    }
}
