namespace ImportNet.Application.UseCases;

public static class FechasPeru
{
    private static readonly TimeSpan Desfase = TimeSpan.FromHours(-5);

    public static DateOnly Hoy()
    {
        return DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(Desfase).DateTime);
    }

    public static DateTime InicioDelDiaUtc(DateOnly dia)
    {
        return new DateTimeOffset(dia.ToDateTime(TimeOnly.MinValue), Desfase).UtcDateTime;
    }
}
