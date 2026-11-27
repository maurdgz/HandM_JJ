using ImportNet.Domain.Entities;

namespace ImportNet.Domain.Interfaces;

public interface IEmpleadoRepository
{
    Task<Empleado?> ObtenerPorUsuarioAsync(string usuarioId, CancellationToken ct = default);
}
