using ImportNet.Domain.Entities;

namespace ImportNet.Application.UseCases.Reportes;

internal sealed class PeriodoReporte
{
    public DateTime DesdeUtc { get; set; }
    public DateTime HastaUtc { get; set; }

    public static PeriodoReporte Desde(DateOnly? desde, DateOnly? hasta, int maximoDias = 366)
    {
        var (inicio, fin) = Fechas(desde, hasta, maximoDias);

        return new PeriodoReporte
        {
            DesdeUtc = FechasPeru.InicioDelDiaUtc(inicio),
            HastaUtc = FechasPeru.InicioDelDiaUtc(fin.AddDays(1))
        };
    }

    public static (DateOnly Desde, DateOnly Hasta) Fechas(DateOnly? desde, DateOnly? hasta, int maximoDias = 366)
    {
        var hoy = FechasPeru.Hoy();
        var fin = hasta ?? hoy;

        DateOnly inicio;
        if (desde != null)
            inicio = desde.Value;
        else if (hasta == null)
            inicio = new DateOnly(hoy.Year, hoy.Month, 1);
        else
            inicio = DateOnly.FromDayNumber(Math.Max(0, fin.DayNumber - 29));

        if (inicio > fin)
            throw new ReglaNegocioException("La fecha inicial no puede ser posterior a la final.");

        if (fin.DayNumber - inicio.DayNumber + 1 > maximoDias)
            throw new ReglaNegocioException($"El periodo no puede superar {maximoDias} días.");

        ValidarFin(fin);
        return (inicio, fin);
    }

    public static PeriodoReporte Dia(DateOnly dia)
    {
        ValidarFin(dia);

        return new PeriodoReporte
        {
            DesdeUtc = FechasPeru.InicioDelDiaUtc(dia),
            HastaUtc = FechasPeru.InicioDelDiaUtc(dia.AddDays(1))
        };
    }

    private static void ValidarFin(DateOnly fin)
    {
        if (fin == DateOnly.MaxValue)
            throw new ReglaNegocioException("La fecha final debe permitir calcular el cierre del día.");
    }
}
