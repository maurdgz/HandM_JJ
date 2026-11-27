using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Categorias.Commands;

public sealed record UpdateCategoriaCommand(int Id, string Nombre) : IRequest<Unit>;

internal sealed class UpdateCategoriaCommandHandler : IRequestHandler<UpdateCategoriaCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCategoriaCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(UpdateCategoriaCommand request, CancellationToken cancellationToken)
    {
        var categoria = await _unitOfWork.Categorias.Obtener(request.Id, cancellationToken);
        if (categoria == null)
            throw new NoEncontradoException("Categoria no encontrada.");

        categoria.Editar(request.Nombre);

        if (await _unitOfWork.Categorias.ExisteNombre(categoria.NombreNormalizado, categoria.Id, cancellationToken))
            throw new ConflictoException("Ya existe una categoría con ese nombre.");

        await _unitOfWork.Guardar(cancellationToken);
        return Unit.Value;
    }
}
