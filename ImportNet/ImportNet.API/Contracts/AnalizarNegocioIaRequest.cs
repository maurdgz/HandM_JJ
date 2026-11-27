namespace ImportNet.API.Contracts;

public sealed record AnalizarNegocioIaRequest(DateOnly? Desde = null, DateOnly? Hasta = null, string? Pregunta = null);
