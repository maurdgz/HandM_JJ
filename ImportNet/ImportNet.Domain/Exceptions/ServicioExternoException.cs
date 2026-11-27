namespace ImportNet.Domain.Exceptions;

public sealed class ServicioExternoException : DomainException
{
    public ServicioExternoException(string mensaje) : base(mensaje)
    {
    }

    public ServicioExternoException(string mensaje, Exception innerException) : base(mensaje, innerException)
    {
    }
}
