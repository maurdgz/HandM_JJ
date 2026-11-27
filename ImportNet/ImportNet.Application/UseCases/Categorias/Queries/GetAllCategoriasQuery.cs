using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Categorias.Queries;

public sealed record GetAllCategoriasQuery : IRequest<IReadOnlyList<Categoria>>;

internal sealed class GetAllCategoriasQueryHandler : IRequestHandler<GetAllCategoriasQuery, IReadOnlyList<Categoria>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAllCategoriasQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<IReadOnlyList<Categoria>> Handle(GetAllCategoriasQuery request, CancellationToken cancellationToken)
    {
        return _unitOfWork.Categorias.Listar(cancellationToken);
    }
}
