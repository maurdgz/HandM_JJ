using ImportNet.Domain.Entities;

namespace ImportNet.Application.UseCases.Reportes;

internal sealed class PeriodoReporte
{
    public DateTime DesdeUtc { get; set; }
    public DateTime HastaUtc { get; set; }

    public static PeriodoReporte Desde(DateOnly? desde, DateOnly? hasta, int maximoDias = 366)
    {
        var hoy = FechasPeru.Hoy();
        var fin = hasta ?? hoy;

        DateOnly inicio;
        if (desde != null)
            inicio = desde.Value;
        else if (hasta == null)
            inicio = new DateOnly(hoy.Year, hoy.Month, 1);
        else
            inicio = fin.AddDays(-29);

        if (inicio > fin)
            throw new ReglaNegocioException("La fecha inicial no puede ser posterior a la final.");

        if (fin.DayNumber - inicio.DayNumber + 1 > maximoDias)
            throw new ReglaNegocioException($"El periodo no puede superar {maximoDias} días.");

        return new PeriodoReporte
        {
            DesdeUtc = FechasPeru.InicioDelDiaUtc(inicio),
            HastaUtc = FechasPeru.InicioDelDiaUtc(fin.AddDays(1))
        };
    }

    public static PeriodoReporte Dia(DateOnly dia)
    {
        return new PeriodoReporte
        {
            DesdeUtc = FechasPeru.InicioDelDiaUtc(dia),
            HastaUtc = FechasPeru.InicioDelDiaUtc(dia.AddDays(1))
        };
    }
}
