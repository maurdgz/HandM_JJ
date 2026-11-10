using MediatR;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
namespace ImportNet.Application.UseCases.Productos.Commands;
public sealed record UpdateProductoCommand(int Id, string Codigo, string Nombre, string? Descripcion, string UnidadMedida, decimal? Peso, decimal PrecioVenta, string? PartidaArancelaria, int CategoriaId, decimal StockMinimo) : IRequest<Unit>;
internal sealed class UpdateProductoCommandHandler(IUnitOfWork uow) : IRequestHandler<UpdateProductoCommand, Unit>
{
    public async Task<Unit> Handle(UpdateProductoCommand r, CancellationToken ct)
    {
        var entidad = await uow.Productos.Obtener(r.Id, ct) ?? throw new NoEncontradoException("Producto no encontrado.");
        entidad.Editar(r.Codigo, r.Nombre, r.Descripcion, r.UnidadMedida, r.Peso, r.PrecioVenta, r.PartidaArancelaria, r.CategoriaId, r.StockMinimo);
        if (!await uow.Categorias.Existe(r.CategoriaId, ct))
            throw new ReglaNegocioException("La categoría no existe.");
        if (await uow.Productos.ExisteCodigo(entidad.Codigo, entidad.Id, ct))
            throw new ConflictoException("Ya existe un producto con ese código.");
        await uow.Guardar(ct);
        return Unit.Value;
    }
}
