namespace ImportNet.Application.UseCases.Ventas;

internal static class FechasPeru
{
    // Perú no usa horario de verano.
    private static readonly TimeSpan Desfase = TimeSpan.FromHours(-5);

    public static DateTime InicioDelDiaUtc(DateOnly dia)
        => new DateTimeOffset(dia.ToDateTime(TimeOnly.MinValue), Desfase).UtcDateTime;
}
