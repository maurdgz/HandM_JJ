using MediatR;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
namespace ImportNet.Application.UseCases.Categorias.Commands;
public sealed record CreateCategoriaCommand(string Nombre) : IRequest<int>;
internal sealed class CreateCategoriaCommandHandler(IUnitOfWork uow) : IRequestHandler<CreateCategoriaCommand, int>
{
    public async Task<int> Handle(CreateCategoriaCommand r, CancellationToken ct)
    {
        var repo = uow.Repository<Categoria>();
        var entidad = new Categoria(r.Nombre);

        if (await repo.Existe(x => x.NombreNormalizado == entidad.NombreNormalizado && x.Id != entidad.Id, ct))
            throw new ConflictoException("Ya existe una categoría con ese nombre.");

        repo.Agregar(entidad);
        await uow.Guardar(ct);
        return entidad.Id;
    }
}
