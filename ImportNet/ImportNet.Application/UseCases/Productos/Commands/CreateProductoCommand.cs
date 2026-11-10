using MediatR;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
namespace ImportNet.Application.UseCases.Productos.Commands;
public sealed record CreateProductoCommand(string Codigo, string Nombre, string? Descripcion, string UnidadMedida, decimal? Peso, decimal PrecioVenta, string? PartidaArancelaria, int CategoriaId, decimal StockMinimo) : IRequest<int>;
internal sealed class CreateProductoCommandHandler(IUnitOfWork uow) : IRequestHandler<CreateProductoCommand, int>
{
    public async Task<int> Handle(CreateProductoCommand r, CancellationToken ct)
    {
        var entidad = new Producto(r.Codigo, r.Nombre, r.Descripcion, r.UnidadMedida, r.Peso, r.PrecioVenta, r.PartidaArancelaria, r.CategoriaId, r.StockMinimo);
        if (!await uow.Categorias.Existe(r.CategoriaId, ct))
            throw new ReglaNegocioException("La categoría no existe.");
        if (await uow.Productos.ExisteCodigo(entidad.Codigo, entidad.Id, ct))
            throw new ConflictoException("Ya existe un producto con ese código.");
        uow.Productos.Agregar(entidad);
        await uow.Guardar(ct);
        return entidad.Id;
    }
}
