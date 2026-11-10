using MediatR;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
namespace ImportNet.Application.UseCases.Categorias.Commands;
public sealed record DeleteCategoriaCommand(int Id) : IRequest<Unit>;
internal sealed class DeleteCategoriaCommandHandler(IUnitOfWork uow) : IRequestHandler<DeleteCategoriaCommand, Unit>
{
    public async Task<Unit> Handle(DeleteCategoriaCommand r, CancellationToken ct)
    {
        var entidad = await uow.Categorias.Obtener(r.Id, ct) ?? throw new NoEncontradoException("Categoria no encontrada.");
        if (await uow.Categorias.TieneProductos(r.Id, ct))
            throw new ConflictoException("La categoría tiene productos y no se puede eliminar.");
        uow.Categorias.Eliminar(entidad);
        await uow.Guardar(ct);
        return Unit.Value;
    }
}
