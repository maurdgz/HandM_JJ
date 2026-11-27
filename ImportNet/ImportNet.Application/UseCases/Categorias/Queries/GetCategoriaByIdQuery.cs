using ImportNet.Domain.Entities;
using ImportNet.Domain.Exceptions;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Categorias.Queries;

public sealed record GetCategoriaByIdQuery(int Id) : IRequest<Categoria>;

internal sealed class GetCategoriaByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetCategoriaByIdQuery, Categoria>
{
    public async Task<Categoria> Handle(GetCategoriaByIdQuery request, CancellationToken cancellationToken)
    {
        return await unitOfWork.Categorias.ObtenerPorIdAsync(request.Id, cancellationToken)
            ?? throw new NoEncontradoException("Categoria no encontrada.");
    }
}
