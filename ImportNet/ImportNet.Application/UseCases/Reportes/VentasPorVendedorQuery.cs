using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Reportes;

public sealed record VentasPorVendedorQuery(DateOnly? Desde = null, DateOnly? Hasta = null) : IRequest<List<VentasPorVendedor>>;

internal sealed class VentasPorVendedorQueryHandler : IRequestHandler<VentasPorVendedorQuery, List<VentasPorVendedor>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUsuarioActual _actor;

    public VentasPorVendedorQueryHandler(IUnitOfWork unitOfWork, IUsuarioActual actor)
    {
        _unitOfWork = unitOfWork;
        _actor = actor;
    }

    public async Task<List<VentasPorVendedor>> Handle(VentasPorVendedorQuery request, CancellationToken cancellationToken)
    {
        await _actor.ExigirAdministrador(cancellationToken);
        var periodo = PeriodoReporte.Desde(request.Desde, request.Hasta);
        return await _unitOfWork.Reportes.VentasPorVendedor(periodo.DesdeUtc, periodo.HastaUtc, cancellationToken);
    }
}
