namespace ImportNet.Application.UseCases.AsistenteIa;

public sealed record AnalisisIaDto(
    DateOnly Desde,
    DateOnly Hasta,
    string ResumenEjecutivo,
    IReadOnlyList<string> Recomendaciones,
    IReadOnlyList<string> AlertasPrioritarias,
    string? RespuestaPregunta,
    string ProveedorIa,
    string Modelo,
    DateTime GeneradoEnUtc);
