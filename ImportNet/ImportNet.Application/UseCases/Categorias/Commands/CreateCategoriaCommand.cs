using ImportNet.Domain.Entities;
using ImportNet.Domain.Exceptions;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Categorias.Commands;

public sealed record CreateCategoriaCommand(string Nombre) : IRequest<int>;

internal sealed class CreateCategoriaCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateCategoriaCommand, int>
{
    public async Task<int> Handle(CreateCategoriaCommand request, CancellationToken cancellationToken)
    {
        var categoria = new Categoria(request.Nombre);

        if (await unitOfWork.Categorias.ExisteNombreAsync(categoria.NombreNormalizado, categoria.Id, cancellationToken))
            throw new ConflictoException("Ya existe una categoría con ese nombre.");

        unitOfWork.Categorias.Agregar(categoria);
        await unitOfWork.GuardarAsync(cancellationToken);
        return categoria.Id;
    }
}
