using ImportNet.Domain.Entities;


namespace ImportNet.Application.UseCases.Reportes;

/// <summary>Rango de fechas en hora de Perú (UTC−5), ambas inclusivas, convertido a UTC.</summary>
internal readonly record struct PeriodoReporte(DateTime DesdeUtc, DateTime HastaUtc)
{
    // Perú no usa horario de verano.
    private static readonly TimeSpan HoraPeru = TimeSpan.FromHours(-5);

    /// <summary>Hasta 366 días. Sin fechas, el mes en curso hasta hoy.</summary>
    public static PeriodoReporte Desde(DateOnly? desde, DateOnly? hasta, int maximoDias = 366)
    {
        var hoy = Hoy();
        var fin = hasta ?? hoy;
        var inicio = desde ?? (hasta is null ? new DateOnly(hoy.Year, hoy.Month, 1) : fin.AddDays(-29));

        if (inicio > fin)
            throw new ReglaNegocioException("La fecha inicial no puede ser posterior a la final.");

        if (fin.DayNumber - inicio.DayNumber + 1 > maximoDias)
            throw new ReglaNegocioException($"El periodo no puede superar {maximoDias} días.");

        return new PeriodoReporte(InicioDelDiaUtc(inicio), InicioDelDiaUtc(fin.AddDays(1)));
    }

    public static PeriodoReporte Dia(DateOnly? fecha)
    {
        var dia = fecha ?? Hoy();
        return new PeriodoReporte(InicioDelDiaUtc(dia), InicioDelDiaUtc(dia.AddDays(1)));
    }

    public static DateOnly Hoy()
        => DateOnly.FromDateTime((DateTimeOffset.UtcNow.ToOffset(HoraPeru)).DateTime);

    private static DateTime InicioDelDiaUtc(DateOnly dia)
        => new DateTimeOffset(dia.ToDateTime(TimeOnly.MinValue), HoraPeru).UtcDateTime;
}
