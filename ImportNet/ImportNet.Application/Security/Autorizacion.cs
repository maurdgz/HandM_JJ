namespace ImportNet.Application.Security;

public static class RolesImportNet
{
    public const string Administrador = "Administrador";
    public const string Vendedor = "Vendedor";
    public static IReadOnlyList<string> Todos { get; } = Array.AsReadOnly(new[] { Administrador, Vendedor });
}

public interface IUsuarioActual
{
    Task ExigirAdministrador(CancellationToken ct);
}

public sealed class AccesoNoPermitidoException(int status) : Exception(status == 401 ? "Debe iniciar sesión." : "Requiere un administrador activo.")
{
    public int Status { get; } = status;
}
