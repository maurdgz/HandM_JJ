using ImportNet.Domain.Exceptions;

namespace ImportNet.Domain.Entities;

public static class CalculadoraCostoPromedio
{
    private const decimal MaxStock = 999999999999999.999m;
    private const decimal MaxCosto = 99999999999999.9999m;
    private const decimal MaxTotal = 9999999999999999.99m;

    public static decimal Calcular(decimal stockActual, decimal costoPromedioActual, decimal cantidadEntrada,
        decimal costoTotalEntradaPen)
    {
        if (stockActual < 0 || stockActual > MaxStock || cantidadEntrada <= 0 || cantidadEntrada > MaxStock - stockActual)
            throw new ReglaNegocioException("Las cantidades de inventario son invalidas.");

        if (decimal.Round(stockActual, 3) != stockActual || decimal.Round(cantidadEntrada, 3) != cantidadEntrada)
            throw new ReglaNegocioException("Las cantidades admiten hasta tres decimales.");

        if (costoPromedioActual < 0 || costoPromedioActual > MaxCosto || costoTotalEntradaPen < 0 || costoTotalEntradaPen > MaxTotal)
            throw new ReglaNegocioException("Los costos o su precision son invalidos.");

        if (decimal.Round(costoPromedioActual, 4) != costoPromedioActual || decimal.Round(costoTotalEntradaPen, 2) != costoTotalEntradaPen)
            throw new ReglaNegocioException("Los costos o su precision son invalidos.");

        decimal promedio;
        try
        {
            var stockFinal = stockActual + cantidadEntrada;
            var valorAnterior = stockActual * costoPromedioActual;
            promedio = decimal.Round((valorAnterior + costoTotalEntradaPen) / stockFinal, 4, MidpointRounding.AwayFromZero);
        }
        catch (OverflowException)
        {
            throw new ReglaNegocioException("Los importes exceden la capacidad del calculo.");
        }

        if (promedio > MaxCosto)
            throw new ReglaNegocioException("El costo promedio excede la precision permitida.");

        return promedio;
    }
}
