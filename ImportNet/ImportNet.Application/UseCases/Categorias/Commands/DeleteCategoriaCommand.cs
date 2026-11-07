using MediatR;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
namespace ImportNet.Application.UseCases.Categorias.Commands;
public sealed record DeleteCategoriaCommand(int Id) : IRequest<Unit>;
internal sealed class DeleteCategoriaCommandHandler(IUnitOfWork uow) : IRequestHandler<DeleteCategoriaCommand, Unit>
{
    public async Task<Unit> Handle(DeleteCategoriaCommand r, CancellationToken ct)
    {
        var repo = uow.Repository<Categoria>();
        var entidad = await repo.Obtener(r.Id, ct) ?? throw new NoEncontradoException("Categoria no encontrada.");
        if (await uow.Repository<Producto>().Existe(x => x.CategoriaId == r.Id, ct))
            throw new ConflictoException("La categoría tiene productos y no se puede eliminar.");
        repo.Eliminar(entidad);
        await uow.Guardar(ct);
        return Unit.Value;
    }
}
