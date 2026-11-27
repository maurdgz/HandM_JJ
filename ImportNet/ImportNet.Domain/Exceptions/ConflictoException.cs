namespace ImportNet.Domain.Exceptions;

public sealed class ConflictoException(string mensaje) : DomainException(mensaje);
