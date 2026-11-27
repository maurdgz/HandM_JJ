using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Reportes;

public sealed record StockBajoMinimoQuery : IRequest<List<ProductoStockBajo>>;

internal sealed class StockBajoMinimoQueryHandler : IRequestHandler<StockBajoMinimoQuery, List<ProductoStockBajo>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUsuarioActual _actor;

    public StockBajoMinimoQueryHandler(IUnitOfWork unitOfWork, IUsuarioActual actor)
    {
        _unitOfWork = unitOfWork;
        _actor = actor;
    }

    public async Task<List<ProductoStockBajo>> Handle(StockBajoMinimoQuery request, CancellationToken cancellationToken)
    {
        await _actor.ExigirAdministrador(cancellationToken);
        return await _unitOfWork.Reportes.StockBajoMinimo(cancellationToken);
    }
}
