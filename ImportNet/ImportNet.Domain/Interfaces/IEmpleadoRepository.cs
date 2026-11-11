using ImportNet.Domain.Entities;

namespace ImportNet.Domain.Interfaces;

public interface IEmpleadoRepository
{
    Task<Empleado?> ObtenerPorUsuario(string usuarioId, CancellationToken ct);
}
