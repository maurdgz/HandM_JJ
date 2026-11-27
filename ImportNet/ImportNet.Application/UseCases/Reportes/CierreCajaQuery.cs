using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Reportes;

public sealed record CierreCajaQuery(DateOnly? Fecha = null) : IRequest<CierreCaja>;

internal sealed class CierreCajaQueryHandler : IRequestHandler<CierreCajaQuery, CierreCaja>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUsuarioActual _actor;

    public CierreCajaQueryHandler(IUnitOfWork unitOfWork, IUsuarioActual actor)
    {
        _unitOfWork = unitOfWork;
        _actor = actor;
    }

    public async Task<CierreCaja> Handle(CierreCajaQuery request, CancellationToken cancellationToken)
    {
        await _actor.ExigirAdministrador(cancellationToken);
        var dia = request.Fecha ?? FechasPeru.Hoy();
        var periodo = PeriodoReporte.Dia(dia);
        return await _unitOfWork.Reportes.CierreCaja(dia, periodo.DesdeUtc, periodo.HastaUtc, null, cancellationToken);
    }
}
