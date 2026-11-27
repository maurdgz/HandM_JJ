namespace ImportNet.Domain.Exceptions;

public sealed class ValidacionException : DomainException
{
    public IReadOnlyDictionary<string, string[]> Errores { get; }

    public ValidacionException(IReadOnlyDictionary<string, string[]> errores)
        : base("Uno o más errores de validación ocurrieron.")
    {
        Errores = errores;
    }
}
