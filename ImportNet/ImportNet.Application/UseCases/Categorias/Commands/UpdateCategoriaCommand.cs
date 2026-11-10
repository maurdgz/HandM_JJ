using MediatR;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
namespace ImportNet.Application.UseCases.Categorias.Commands;
public sealed record UpdateCategoriaCommand(int Id, string Nombre) : IRequest<Unit>;
internal sealed class UpdateCategoriaCommandHandler(IUnitOfWork uow) : IRequestHandler<UpdateCategoriaCommand, Unit>
{
    public async Task<Unit> Handle(UpdateCategoriaCommand r, CancellationToken ct)
    {
        var entidad = await uow.Categorias.Obtener(r.Id, ct) ?? throw new NoEncontradoException("Categoria no encontrada.");
        entidad.Editar(r.Nombre);
        if (await uow.Categorias.ExisteNombre(entidad.NombreNormalizado, entidad.Id, ct))
            throw new ConflictoException("Ya existe una categoría con ese nombre.");
        await uow.Guardar(ct);
        return Unit.Value;
    }
}
