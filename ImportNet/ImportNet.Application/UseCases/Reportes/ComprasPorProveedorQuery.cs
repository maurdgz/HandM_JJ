using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Reportes;

public sealed record ComprasPorProveedorQuery(DateOnly? Desde = null, DateOnly? Hasta = null) : IRequest<List<ComprasPorProveedor>>;

internal sealed class ComprasPorProveedorQueryHandler(IUnitOfWork unitOfWork, IUsuarioActual actor)
    : IRequestHandler<ComprasPorProveedorQuery, List<ComprasPorProveedor>>
{
    public async Task<List<ComprasPorProveedor>> Handle(ComprasPorProveedorQuery request, CancellationToken cancellationToken)
    {
        await actor.ExigirAdministradorAsync(cancellationToken);
        var periodo = PeriodoReporte.Desde(request.Desde, request.Hasta);
        return await unitOfWork.Reportes.ComprasPorProveedorAsync(periodo.DesdeUtc, periodo.HastaUtc, cancellationToken);
    }
}
