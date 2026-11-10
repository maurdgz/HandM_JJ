using MediatR;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
namespace ImportNet.Application.UseCases.Categorias.Queries;
public sealed record GetCategoriaByIdQuery(int Id) : IRequest<Categoria>;
internal sealed class GetCategoriaByIdQueryHandler(IUnitOfWork uow) : IRequestHandler<GetCategoriaByIdQuery, Categoria>
{
    public async Task<Categoria> Handle(GetCategoriaByIdQuery r, CancellationToken ct)
        => await uow.Categorias.Obtener(r.Id, ct) ?? throw new NoEncontradoException("Categoria no encontrada.");
}
