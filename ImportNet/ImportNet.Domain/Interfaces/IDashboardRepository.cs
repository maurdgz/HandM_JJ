using ImportNet.Domain.Entities;

namespace ImportNet.Domain.Interfaces;

/// <summary>Lectura agregada, sin seguimiento, del resumen administrativo.</summary>
public interface IDashboardRepository
{
    Task<ResumenDashboard> Resumen(DateOnly fecha, DateTime inicioDiaUtc, DateTime finDiaUtc,
        DateTime inicioMesUtc, CancellationToken ct);
}
