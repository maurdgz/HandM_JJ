namespace ImportNet.Domain.Exceptions;

public sealed class NoEncontradoException(string mensaje) : DomainException(mensaje);
