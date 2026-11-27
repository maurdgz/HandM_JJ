using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Categorias.Commands;

public sealed record DeleteCategoriaCommand(int Id) : IRequest<Unit>;

internal sealed class DeleteCategoriaCommandHandler : IRequestHandler<DeleteCategoriaCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCategoriaCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteCategoriaCommand request, CancellationToken cancellationToken)
    {
        var categoria = await _unitOfWork.Categorias.Obtener(request.Id, cancellationToken);
        if (categoria == null)
            throw new NoEncontradoException("Categoria no encontrada.");

        if (await _unitOfWork.Categorias.TieneProductos(request.Id, cancellationToken))
            throw new ConflictoException("La categoría tiene productos y no se puede eliminar.");

        _unitOfWork.Categorias.Eliminar(categoria);
        await _unitOfWork.Guardar(cancellationToken);
        return Unit.Value;
    }
}
