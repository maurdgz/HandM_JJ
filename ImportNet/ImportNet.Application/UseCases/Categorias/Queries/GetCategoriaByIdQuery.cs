using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Categorias.Queries;

public sealed record GetCategoriaByIdQuery(int Id) : IRequest<Categoria>;

internal sealed class GetCategoriaByIdQueryHandler : IRequestHandler<GetCategoriaByIdQuery, Categoria>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetCategoriaByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Categoria> Handle(GetCategoriaByIdQuery request, CancellationToken cancellationToken)
    {
        var categoria = await _unitOfWork.Categorias.Obtener(request.Id, cancellationToken);
        if (categoria == null)
            throw new NoEncontradoException("Categoria no encontrada.");

        return categoria;
    }
}
