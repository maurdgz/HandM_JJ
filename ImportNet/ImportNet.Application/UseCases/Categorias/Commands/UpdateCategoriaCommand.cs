using ImportNet.Domain.Exceptions;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Categorias.Commands;

public sealed record UpdateCategoriaCommand(int Id, string Nombre) : IRequest<Unit>;

internal sealed class UpdateCategoriaCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateCategoriaCommand, Unit>
{
    public async Task<Unit> Handle(UpdateCategoriaCommand request, CancellationToken cancellationToken)
    {
        var categoria = await unitOfWork.Categorias.ObtenerPorIdAsync(request.Id, cancellationToken)
            ?? throw new NoEncontradoException("Categoria no encontrada.");

        categoria.Editar(request.Nombre);

        if (await unitOfWork.Categorias.ExisteNombreAsync(categoria.NombreNormalizado, categoria.Id, cancellationToken))
            throw new ConflictoException("Ya existe una categoría con ese nombre.");

        await unitOfWork.GuardarAsync(cancellationToken);
        return Unit.Value;
    }
}
