using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Categorias.Queries;

public sealed record GetAllCategoriasQuery : IRequest<IReadOnlyList<Categoria>>;

internal sealed class GetAllCategoriasQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAllCategoriasQuery, IReadOnlyList<Categoria>>
{
    public Task<IReadOnlyList<Categoria>> Handle(GetAllCategoriasQuery request, CancellationToken cancellationToken)
        => unitOfWork.Categorias.ListarAsync(cancellationToken);
}
