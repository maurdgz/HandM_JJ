using ImportNet.Domain.Exceptions;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Categorias.Commands;

public sealed record DeleteCategoriaCommand(int Id) : IRequest<Unit>;

internal sealed class DeleteCategoriaCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteCategoriaCommand, Unit>
{
    public async Task<Unit> Handle(DeleteCategoriaCommand request, CancellationToken cancellationToken)
    {
        var categoria = await unitOfWork.Categorias.ObtenerPorIdAsync(request.Id, cancellationToken)
            ?? throw new NoEncontradoException("Categoria no encontrada.");

        if (await unitOfWork.Categorias.TieneProductosAsync(request.Id, cancellationToken))
            throw new ConflictoException("La categoría tiene productos y no se puede eliminar.");

        unitOfWork.Categorias.Eliminar(categoria);
        await unitOfWork.GuardarAsync(cancellationToken);
        return Unit.Value;
    }
}
