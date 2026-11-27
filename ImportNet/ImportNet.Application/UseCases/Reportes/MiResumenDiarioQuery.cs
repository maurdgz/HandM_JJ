using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Reportes;

public sealed record MiResumenDiarioQuery(DateOnly? Fecha = null) : IRequest<CierreCaja>;

internal sealed class MiResumenDiarioQueryHandler(IUnitOfWork unitOfWork, IUsuarioActual actor)
    : IRequestHandler<MiResumenDiarioQuery, CierreCaja>
{
    public async Task<CierreCaja> Handle(MiResumenDiarioQuery request, CancellationToken cancellationToken)
    {
        var empleado = await unitOfWork.Empleados.ObtenerEmpleadoActivoAsync(actor, cancellationToken);
        var dia = request.Fecha ?? FechasPeru.Hoy();
        var periodo = PeriodoReporte.Dia(dia);
        return await unitOfWork.Reportes.CierreCajaAsync(dia, periodo.DesdeUtc, periodo.HastaUtc, empleado.Id, cancellationToken);
    }
}
