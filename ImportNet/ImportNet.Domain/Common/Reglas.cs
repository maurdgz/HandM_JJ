using ImportNet.Domain.Exceptions;

namespace ImportNet.Domain.Common;

/// <summary>
/// Validaciones transversales de invariantes del dominio.
/// </summary>
public static class Reglas
{
    public static string Texto(string? valor, int max, string campo)
    {
        if (string.IsNullOrWhiteSpace(valor) || valor.Trim().Length > max)
            throw new ReglaNegocioException($"{campo} es obligatorio y admite hasta {max} caracteres.");

        return valor.Trim();
    }
}
