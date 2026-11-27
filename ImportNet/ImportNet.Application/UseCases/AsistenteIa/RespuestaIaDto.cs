namespace ImportNet.Application.UseCases.AsistenteIa;

public sealed record RespuestaIaDto(
    string ResumenEjecutivo,
    IReadOnlyList<string> Recomendaciones,
    IReadOnlyList<string> AlertasPrioritarias,
    string? RespuestaPregunta,
    string ProveedorIa,
    string Modelo);
