namespace ImportNet.Domain.Entities;
public sealed record CostoLineaImportacion(decimal MercaderiaPen, decimal FletePen, decimal AduanaPen)
{
    public decimal TotalPen => MercaderiaPen + FletePen + AduanaPen;
}
public sealed record CostoImportacion(decimal SubtotalUsd, decimal MercaderiaPen, decimal FletePen, decimal AduanaPen, IReadOnlyList<CostoLineaImportacion> Lineas)
{
    public decimal TotalPen => MercaderiaPen + FletePen + AduanaPen;
}
/// <summary>Importes AwayFromZero a 2 decimales. Mayor resto; empates por orden de entrada.</summary>
public static class CalculadoraCostoImportacion
{
    public static CostoImportacion Calcular(IReadOnlyList<LineaCompra> lineas, decimal tipoCambio, decimal fletePen, decimal aduanaPen)
    {
        if (lineas is null || lineas.Count == 0 || lineas.Count > 100 || lineas.Any(x => x is null || x.Cantidad <= 0 || x.Cantidad > 1000000 || x.CostoUnitarioUsd <= 0 || x.CostoUnitarioUsd > 1000000) || tipoCambio <= 0 || tipoCambio > 1000 || fletePen < 0 || aduanaPen < 0 || fletePen > 1000000000000m || aduanaPen > 1000000000000m || Redondear(fletePen) != fletePen || Redondear(aduanaPen) != aduanaPen)
            throw new ReglaNegocioException("Base de prorrateo o importes invalidos.");
        var pesos = lineas.Select(x => x.Cantidad * x.CostoUnitarioUsd).ToArray();
        var subtotal = pesos.Sum(); var mercaderia = Redondear(subtotal * tipoCambio);
        if (mercaderia + fletePen + aduanaPen > 9999999999999999.99m) throw new ReglaNegocioException("El total excede la precision monetaria permitida.");
        var m = Repartir(mercaderia, pesos); var f = Repartir(fletePen, pesos); var a = Repartir(aduanaPen, pesos);
        return new(subtotal, mercaderia, fletePen, aduanaPen, Enumerable.Range(0, pesos.Length).Select(i => new CostoLineaImportacion(m[i], f[i], a[i])).ToArray());
    }
    private static decimal Redondear(decimal valor) => decimal.Round(valor, 2, MidpointRounding.AwayFromZero);
    private static decimal[] Repartir(decimal total, decimal[] pesos)
    {
        var suma = pesos.Sum();
        // Divide first to keep intermediate values within decimal for large purchases.
        var exactos = pesos.Select(p => total * (p / suma) * 100).ToArray();
        var centavos = exactos.Select(decimal.Floor).ToArray();
        var pendientes = checked((int)(total * 100 - centavos.Sum()));
        var orden = Enumerable.Range(0, pesos.Length).OrderByDescending(i => exactos[i] - centavos[i]).ThenBy(i => i).ToArray();
        for (var i = 0; i < pendientes; i++) centavos[orden[i % orden.Length]]++;
        return centavos.Select(c => c / 100).ToArray();
    }
}
