using MediatR;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
namespace ImportNet.Application.UseCases.Categorias.Queries;
public sealed record GetAllCategoriasQuery() : IRequest<IReadOnlyList<Categoria>>;
internal sealed class GetAllCategoriasQueryHandler(IUnitOfWork uow) : IRequestHandler<GetAllCategoriasQuery, IReadOnlyList<Categoria>>
{
    public async Task<IReadOnlyList<Categoria>> Handle(GetAllCategoriasQuery r, CancellationToken ct)
        => await uow.Repository<Categoria>().Listar(ct);
}
