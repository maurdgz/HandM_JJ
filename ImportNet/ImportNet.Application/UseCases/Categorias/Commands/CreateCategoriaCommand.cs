using MediatR;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
namespace ImportNet.Application.UseCases.Categorias.Commands;
public sealed record CreateCategoriaCommand(string Nombre) : IRequest<int>;
internal sealed class CreateCategoriaCommandHandler(IUnitOfWork uow) : IRequestHandler<CreateCategoriaCommand, int>
{
    public async Task<int> Handle(CreateCategoriaCommand r, CancellationToken ct)
    {
        var entidad = new Categoria(r.Nombre);
        if (await uow.Categorias.ExisteNombre(entidad.NombreNormalizado, entidad.Id, ct))
            throw new ConflictoException("Ya existe una categoría con ese nombre.");
        uow.Categorias.Agregar(entidad);
        await uow.Guardar(ct);
        return entidad.Id;
    }
}
