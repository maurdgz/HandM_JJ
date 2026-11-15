using ImportNet.Domain.Entities;
using ImportNet.Domain.ReadModels;

namespace ImportNet.Domain.Interfaces;

public interface IVentaRepository : IGenericRepository<Venta>
{
    Task<IReadOnlyList<VentaResumen>> Historial(
        DateTime desdeUtc,
        DateTime hastaUtcExclusivo,
        int? empleadoId,
        CancellationToken ct);

    Task<NotaVenta?> Nota(
        int id,
        int? empleadoId,
        CancellationToken ct);
}