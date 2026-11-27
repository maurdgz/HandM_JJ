using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Reportes;

public sealed record StockBajoMinimoQuery : IRequest<List<ProductoStockBajo>>;

internal sealed class StockBajoMinimoQueryHandler(IUnitOfWork unitOfWork, IUsuarioActual actor)
    : IRequestHandler<StockBajoMinimoQuery, List<ProductoStockBajo>>
{
    public async Task<List<ProductoStockBajo>> Handle(StockBajoMinimoQuery request, CancellationToken cancellationToken)
    {
        await actor.ExigirAdministradorAsync(cancellationToken);
        return await unitOfWork.Reportes.StockBajoMinimoAsync(cancellationToken);
    }
}
