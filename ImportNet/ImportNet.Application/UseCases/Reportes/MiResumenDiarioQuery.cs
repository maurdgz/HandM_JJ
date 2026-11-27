using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Reportes;

public sealed record MiResumenDiarioQuery(DateOnly? Fecha = null) : IRequest<CierreCaja>;

internal sealed class MiResumenDiarioQueryHandler : IRequestHandler<MiResumenDiarioQuery, CierreCaja>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUsuarioActual _actor;
    private readonly IEmpleadoRepository _empleados;

    public MiResumenDiarioQueryHandler(IUnitOfWork unitOfWork, IUsuarioActual actor, IEmpleadoRepository empleados)
    {
        _unitOfWork = unitOfWork;
        _actor = actor;
        _empleados = empleados;
    }

    public async Task<CierreCaja> Handle(MiResumenDiarioQuery request, CancellationToken cancellationToken)
    {
        var empleado = await _empleados.ObtenerEmpleadoActivo(_actor, cancellationToken);
        var dia = request.Fecha ?? FechasPeru.Hoy();
        var periodo = PeriodoReporte.Dia(dia);
        return await _unitOfWork.Reportes.CierreCaja(dia, periodo.DesdeUtc, periodo.HastaUtc, empleado.Id, cancellationToken);
    }
}
