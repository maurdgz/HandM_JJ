using ImportNet.Application.UseCases.AsistenteIa;

namespace ImportNet.Application.Interfaces;

/// <summary>
/// Puerto de salida para análisis de negocio asistido por un proveedor externo de IA.
/// </summary>
public interface IAsistenteIaService
{
    Task<RespuestaIaDto> AnalizarNegocioAsync(ContextoNegocioIa contexto, string? pregunta, CancellationToken ct = default);
}
