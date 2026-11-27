using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Productos.Queries;

public sealed record GetAllProductosQuery : IRequest<IReadOnlyList<Producto>>;

internal sealed class GetAllProductosQueryHandler : IRequestHandler<GetAllProductosQuery, IReadOnlyList<Producto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAllProductosQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<IReadOnlyList<Producto>> Handle(GetAllProductosQuery request, CancellationToken cancellationToken)
    {
        return _unitOfWork.Productos.Listar(cancellationToken);
    }
}
