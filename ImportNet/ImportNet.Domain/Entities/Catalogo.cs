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
    public decimal Peso { get; private set; }
    public decimal PrecioUsd { get; private set; }
    public string PartidaArancelaria { get; private set; } = "";
    public int CategoriaId { get; private set; }
    public decimal StockMinimo { get; private set; }
    private Producto() { }
    public Producto(string codigo, string nombre, string? descripcion, string unidadMedida,
        decimal peso, decimal precioUsd, string partidaArancelaria, int categoriaId, decimal stockMinimo)
        => Editar(codigo, nombre, descripcion, unidadMedida, peso, precioUsd, partidaArancelaria, categoriaId, stockMinimo);

    public void Editar(string codigo, string nombre, string? descripcion, string unidadMedida,
        decimal peso, decimal precioUsd, string partidaArancelaria, int categoriaId, decimal stockMinimo)
    {
        if (peso <= 0 || peso > 999999999999999.999m || decimal.Round(peso, 3) != peso)
            throw new ReglaNegocioException("Peso debe ser positivo y admitir hasta tres decimales.");
        if (precioUsd < 0 || precioUsd > 9999999999999999.99m || decimal.Round(precioUsd, 2) != precioUsd)
            throw new ReglaNegocioException("Precio USD debe ser no negativo y admitir hasta dos decimales.");
        if (stockMinimo < 0 || stockMinimo > 999999999999999.999m || decimal.Round(stockMinimo, 3) != stockMinimo || categoriaId <= 0)
            throw new ReglaNegocioException("Categoría o stock mínimo inválidos.");
        if (descripcion?.Length > 1000) throw new ReglaNegocioException("Descripción admite hasta 1000 caracteres.");
        // Validate everything before mutating an existing tracked entity.
        var codigoValido = Reglas.Texto(codigo, 40, "Código").ToUpperInvariant();
        var nombreValido = Reglas.Texto(nombre, 200, "Nombre");
        var unidadValida = Reglas.Texto(unidadMedida, 30, "Unidad de medida");
        var partidaValida = Reglas.Texto(partidaArancelaria, 20, "Partida arancelaria");
        Codigo = codigoValido; Nombre = nombreValido; UnidadMedida = unidadValida;
        PartidaArancelaria = partidaValida; Descripcion = descripcion?.Trim();
        Peso = peso; PrecioUsd = precioUsd; CategoriaId = categoriaId; StockMinimo = stockMinimo;
    }
}
