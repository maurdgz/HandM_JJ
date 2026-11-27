using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Reportes;

public sealed record CierreCajaQuery(DateOnly? Fecha = null) : IRequest<CierreCaja>;

internal sealed class CierreCajaQueryHandler(IUnitOfWork unitOfWork, IUsuarioActual actor)
    : IRequestHandler<CierreCajaQuery, CierreCaja>
{
    public async Task<CierreCaja> Handle(CierreCajaQuery request, CancellationToken cancellationToken)
    {
        await actor.ExigirAdministradorAsync(cancellationToken);
        var dia = request.Fecha ?? FechasPeru.Hoy();
        var periodo = PeriodoReporte.Dia(dia);
        return await unitOfWork.Reportes.CierreCajaAsync(dia, periodo.DesdeUtc, periodo.HastaUtc, null, cancellationToken);
    }
}
