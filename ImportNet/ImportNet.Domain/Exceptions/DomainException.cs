namespace ImportNet.Domain.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException(string mensaje) : base(mensaje)
    {
    }

    protected DomainException(string mensaje, Exception innerException) : base(mensaje, innerException)
    {
    }
}
