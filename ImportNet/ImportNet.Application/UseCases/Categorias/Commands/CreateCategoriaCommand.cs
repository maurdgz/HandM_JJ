using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Categorias.Commands;

public sealed record CreateCategoriaCommand(string Nombre) : IRequest<int>;

internal sealed class CreateCategoriaCommandHandler : IRequestHandler<CreateCategoriaCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateCategoriaCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(CreateCategoriaCommand request, CancellationToken cancellationToken)
    {
        var categoria = new Categoria(request.Nombre);

        if (await _unitOfWork.Categorias.ExisteNombre(categoria.NombreNormalizado, categoria.Id, cancellationToken))
            throw new ConflictoException("Ya existe una categoría con ese nombre.");

        _unitOfWork.Categorias.Agregar(categoria);
        await _unitOfWork.Guardar(cancellationToken);
        return categoria.Id;
    }
}
