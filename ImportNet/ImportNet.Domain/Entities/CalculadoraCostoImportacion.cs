namespace ImportNet.Domain.Entities;

public sealed record CostoLineaImportacion(decimal MercaderiaPen, decimal FletePen, decimal AduanaPen);

public static class CalculadoraCostoImportacion
{
    public static List<CostoLineaImportacion> Calcular(IReadOnlyList<LineaCompra> lineas, decimal tipoCambio,
        decimal fletePen, decimal aduanaPen)
    {
        if (lineas == null || lineas.Count == 0 || lineas.Count > 100)
            throw new ReglaNegocioException("Base de prorrateo o importes invalidos.");

        foreach (var linea in lineas)
        {
            if (linea == null || linea.Cantidad <= 0 || linea.Cantidad > 1000000
                || linea.CostoUnitarioUsd <= 0 || linea.CostoUnitarioUsd > 1000000)
                throw new ReglaNegocioException("Base de prorrateo o importes invalidos.");
        }

        if (tipoCambio <= 0 || tipoCambio > 1000)
            throw new ReglaNegocioException("Base de prorrateo o importes invalidos.");

        if (fletePen < 0 || aduanaPen < 0 || fletePen > 1000000000000m || aduanaPen > 1000000000000m)
            throw new ReglaNegocioException("Base de prorrateo o importes invalidos.");

        if (Redondear(fletePen) != fletePen || Redondear(aduanaPen) != aduanaPen)
            throw new ReglaNegocioException("Base de prorrateo o importes invalidos.");

        var pesos = lineas.Select(x => x.Cantidad * x.CostoUnitarioUsd).ToList();
        var mercaderia = Redondear(pesos.Sum() * tipoCambio);

        if (mercaderia + fletePen + aduanaPen > 9999999999999999.99m)
            throw new ReglaNegocioException("El total excede la precision monetaria permitida.");

        var mercaderiaPorLinea = Repartir(mercaderia, pesos);
        var fletePorLinea = Repartir(fletePen, pesos);
        var aduanaPorLinea = Repartir(aduanaPen, pesos);

        var resultado = new List<CostoLineaImportacion>();
        for (var i = 0; i < pesos.Count; i++)
            resultado.Add(new CostoLineaImportacion(mercaderiaPorLinea[i], fletePorLinea[i], aduanaPorLinea[i]));

        return resultado;
    }

    private static decimal Redondear(decimal valor)
    {
        return decimal.Round(valor, 2, MidpointRounding.AwayFromZero);
    }

    private static List<decimal> Repartir(decimal total, List<decimal> pesos)
    {
        var suma = pesos.Sum();
        var exactos = pesos.Select(p => total * (p / suma) * 100).ToList();
        var centavos = exactos.Select(decimal.Floor).ToList();
        var pendientes = (int)(total * 100 - centavos.Sum());

        var orden = Enumerable.Range(0, pesos.Count)
            .OrderByDescending(i => exactos[i] - centavos[i])
            .ThenBy(i => i)
            .ToList();

        for (var i = 0; i < pendientes; i++)
            centavos[orden[i % orden.Count]]++;

        return centavos.Select(c => c / 100).ToList();
    }
}
