using MediatR;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
namespace ImportNet.Application.UseCases.Productos.Commands;
public sealed record CreateProductoCommand(string Codigo, string Nombre, string? Descripcion, string UnidadMedida, decimal Peso, decimal PrecioUsd, string PartidaArancelaria, int CategoriaId, decimal StockMinimo) : IRequest<int>;
internal sealed class CreateProductoCommandHandler(IUnitOfWork uow) : IRequestHandler<CreateProductoCommand, int>
{
    public async Task<int> Handle(CreateProductoCommand r, CancellationToken ct)
    {
        var repo = uow.Repository<Producto>();
        var entidad = new Producto(r.Codigo, r.Nombre, r.Descripcion, r.UnidadMedida, r.Peso, r.PrecioUsd, r.PartidaArancelaria, r.CategoriaId, r.StockMinimo);

        if (!await uow.Repository<Categoria>().Existe(x => x.Id == r.CategoriaId, ct))
            throw new ReglaNegocioException("La categoría no existe.");
        var codigo = r.Codigo?.Trim().ToUpperInvariant();
        if (await repo.Existe(x => x.Codigo == codigo && x.Id != entidad.Id, ct))
            throw new ConflictoException("Ya existe un producto con ese código.");

        repo.Agregar(entidad);
        await uow.Guardar(ct);
        return entidad.Id;
    }
}
