using ImportNet.Domain.Common;
using ImportNet.Domain.Exceptions;

namespace ImportNet.Domain.Entities;

public sealed class Producto : Entity
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

    public List<Inventario> Inventarios { get; private set; } = [];

    private Producto()
    {
    }

    public Producto(string codigo, string nombre, string? descripcion, string unidadMedida, decimal? peso,
        decimal precioVenta, string? partidaArancelaria, int categoriaId, decimal stockMinimo)
    {
        Editar(codigo, nombre, descripcion, unidadMedida, peso, precioVenta, partidaArancelaria, categoriaId, stockMinimo);
    }

    public void Editar(string codigo, string nombre, string? descripcion, string unidadMedida, decimal? peso,
        decimal precioVenta, string? partidaArancelaria, int categoriaId, decimal stockMinimo)
    {
        if (peso != null && (peso <= 0 || peso > 999999999999999.999m || decimal.Round(peso.Value, 3) != peso))
            throw new ReglaNegocioException("Peso debe ser positivo y admitir hasta tres decimales.");

        if (precioVenta < 0 || precioVenta > 9999999999999999.99m || decimal.Round(precioVenta, 2) != precioVenta)
            throw new ReglaNegocioException("Precio de venta debe ser no negativo y admitir hasta dos decimales.");

        if (stockMinimo < 0 || stockMinimo > 999999999999999.999m || decimal.Round(stockMinimo, 3) != stockMinimo || categoriaId <= 0)
            throw new ReglaNegocioException("Categoría o stock mínimo inválidos.");

        if (descripcion != null && descripcion.Length > 1000)
            throw new ReglaNegocioException("Descripción admite hasta 1000 caracteres.");

        if (partidaArancelaria != null && partidaArancelaria.Trim().Length > 20)
            throw new ReglaNegocioException("Partida arancelaria admite hasta 20 caracteres.");

        Codigo = Reglas.Texto(codigo, 40, "Código").ToUpperInvariant();
        Nombre = Reglas.Texto(nombre, 200, "Nombre");
        UnidadMedida = Reglas.Texto(unidadMedida, 30, "Unidad de medida");
        PartidaArancelaria = string.IsNullOrWhiteSpace(partidaArancelaria) ? null : partidaArancelaria.Trim();
        Descripcion = descripcion?.Trim();
        Peso = peso;
        PrecioVenta = precioVenta;
        CategoriaId = categoriaId;
        StockMinimo = stockMinimo;
    }

    public void RegistrarCostoImportacion(decimal? importeUsd, decimal? tipoCambio)
    {
        var valido = importeUsd.HasValue == tipoCambio.HasValue;

        if (importeUsd != null && (importeUsd < 0 || importeUsd > 9999999999999999.99m || decimal.Round(importeUsd.Value, 2) != importeUsd))
            valido = false;

        if (tipoCambio != null && (tipoCambio <= 0 || tipoCambio > 999999999999.999999m || decimal.Round(tipoCambio.Value, 6) != tipoCambio))
            valido = false;

        if (!valido)
            throw new ReglaNegocioException("Indique costo USD no negativo (2 decimales) y tipo de cambio PEN/USD positivo (6 decimales), o deje ambos vacíos.");

        CostoImportacionUsd = importeUsd;
        TipoCambioImportacion = tipoCambio;
    }

    public void CambiarEstado(bool activo)
    {
        Activo = activo;
    }

    public void ActualizarCostoPromedioPorRecepcion(decimal stockActual, decimal cantidadEntrada, decimal costoTotalEntradaPen)
    {
        if (!Activo)
            throw new ReglaNegocioException("No se puede recibir mercadería de un producto inactivo.");

        CostoPromedio = CalculadoraCostoPromedio.Calcular(stockActual, CostoPromedio, cantidadEntrada, costoTotalEntradaPen);
    }
}
