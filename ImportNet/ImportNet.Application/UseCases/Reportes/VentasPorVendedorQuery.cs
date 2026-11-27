using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Reportes;

public sealed record VentasPorVendedorQuery(DateOnly? Desde = null, DateOnly? Hasta = null) : IRequest<List<VentasPorVendedor>>;

internal sealed class VentasPorVendedorQueryHandler(IUnitOfWork unitOfWork, IUsuarioActual actor)
    : IRequestHandler<VentasPorVendedorQuery, List<VentasPorVendedor>>
{
    public async Task<List<VentasPorVendedor>> Handle(VentasPorVendedorQuery request, CancellationToken cancellationToken)
    {
        await actor.ExigirAdministradorAsync(cancellationToken);
        var periodo = PeriodoReporte.Desde(request.Desde, request.Hasta);
        return await unitOfWork.Reportes.VentasPorVendedorAsync(periodo.DesdeUtc, periodo.HastaUtc, cancellationToken);
    }
}
