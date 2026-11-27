namespace ImportNet.Domain.Exceptions;

public sealed class ReglaNegocioException(string mensaje) : DomainException(mensaje);
