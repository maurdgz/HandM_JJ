using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;

namespace ImportNet.Application.Security;

public static class RolesImportNet
{
    public const string Administrador = "Administrador";
    public const string Vendedor = "Vendedor";
    public static IReadOnlyList<string> Todos { get; } = [Administrador, Vendedor];
}

public interface IUsuarioActual
{
    string? Id { get; }
    Task ExigirAdministradorAsync(CancellationToken ct = default);
    Task<bool> EsAdministradorAsync(CancellationToken ct = default);
}

public sealed class AccesoNoPermitidoException : Exception
{
    public int Status { get; }

    public AccesoNoPermitidoException(int status)
        : base(status == 401 ? "Debe iniciar sesión." : "Requiere un administrador activo.")
    {
        Status = status;
    }
}

public static class EmpleadoActualExtensions
{
    public static async Task<Empleado> ObtenerEmpleadoActivoAsync(this IEmpleadoRepository empleados,
        IUsuarioActual usuario, CancellationToken ct = default)
    {
        if (usuario.Id == null)
            throw new AccesoNoPermitidoException(401);

        var empleado = await empleados.ObtenerPorUsuarioAsync(usuario.Id, ct);
        if (empleado == null || !empleado.Activo)
            throw new AccesoNoPermitidoException(403);

        return empleado;
    }
}
