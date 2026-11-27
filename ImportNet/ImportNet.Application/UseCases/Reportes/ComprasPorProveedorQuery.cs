using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Reportes;

public sealed record ComprasPorProveedorQuery(DateOnly? Desde = null, DateOnly? Hasta = null) : IRequest<List<ComprasPorProveedor>>;

internal sealed class ComprasPorProveedorQueryHandler : IRequestHandler<ComprasPorProveedorQuery, List<ComprasPorProveedor>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUsuarioActual _actor;

    public ComprasPorProveedorQueryHandler(IUnitOfWork unitOfWork, IUsuarioActual actor)
    {
        _unitOfWork = unitOfWork;
        _actor = actor;
    }

    public async Task<List<ComprasPorProveedor>> Handle(ComprasPorProveedorQuery request, CancellationToken cancellationToken)
    {
        await _actor.ExigirAdministrador(cancellationToken);
        var periodo = PeriodoReporte.Desde(request.Desde, request.Hasta);
        return await _unitOfWork.Reportes.ComprasPorProveedor(periodo.DesdeUtc, periodo.HastaUtc, cancellationToken);
    }
}
