namespace ImportNet.Application.Security;

public static class RolesImportNet
{
    public const string Administrador = "Administrador";
    public const string Vendedor = "Vendedor";
    public static IReadOnlyList<string> Todos { get; } = Array.AsReadOnly(new[] { Administrador, Vendedor });
}

public interface IUsuarioActual
{
    string? Id { get; }
    Task ExigirAdministrador(CancellationToken ct);

    async Task<bool> EsAdministrador(CancellationToken ct)
    {
        try
        {
            await ExigirAdministrador(ct);
            return true;
        }
        catch (AccesoNoPermitidoException ex) when (ex.Status == 403)
        {
            return false;
        }
    }
}

public sealed class AccesoNoPermitidoException(int status) : Exception(status == 401 ? "Debe iniciar sesión." : "Requiere un administrador activo.")
{
    public int Status { get; } = status;
}
