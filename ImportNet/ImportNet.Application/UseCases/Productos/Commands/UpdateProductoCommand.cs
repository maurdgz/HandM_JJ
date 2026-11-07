using MediatR;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
namespace ImportNet.Application.UseCases.Productos.Commands;
public sealed record UpdateProductoCommand(int Id, string Codigo, string Nombre, string? Descripcion, string UnidadMedida, decimal Peso, decimal PrecioUsd, string PartidaArancelaria, int CategoriaId, decimal StockMinimo) : IRequest<Unit>;
internal sealed class UpdateProductoCommandHandler(IUnitOfWork uow) : IRequestHandler<UpdateProductoCommand, Unit>
{
    public async Task<Unit> Handle(UpdateProductoCommand r, CancellationToken ct)
    {
        var repo = uow.Repository<Producto>();
        var entidad = await repo.Obtener(r.Id, ct) ?? throw new NoEncontradoException("Producto no encontrada.");
        entidad.Editar(r.Codigo, r.Nombre, r.Descripcion, r.UnidadMedida, r.Peso, r.PrecioUsd, r.PartidaArancelaria, r.CategoriaId, r.StockMinimo);

        if (!await uow.Repository<Categoria>().Existe(x => x.Id == r.CategoriaId, ct))
            throw new ReglaNegocioException("La categoría no existe.");
        var codigo = r.Codigo?.Trim().ToUpperInvariant();
        if (await repo.Existe(x => x.Codigo == codigo && x.Id != entidad.Id, ct))
            throw new ConflictoException("Ya existe un producto con ese código.");


        await uow.Guardar(ct);
        return Unit.Value;
    }
}
