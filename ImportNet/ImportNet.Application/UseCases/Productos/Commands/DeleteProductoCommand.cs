using MediatR;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
namespace ImportNet.Application.UseCases.Productos.Commands;
public sealed record DeleteProductoCommand(int Id) : IRequest<Unit>;
internal sealed class DeleteProductoCommandHandler(IUnitOfWork uow) : IRequestHandler<DeleteProductoCommand, Unit>
{
    public async Task<Unit> Handle(DeleteProductoCommand r, CancellationToken ct)
    {
        var repo = uow.Repository<Producto>();
        var entidad = await repo.Obtener(r.Id, ct) ?? throw new NoEncontradoException("Producto no encontrada.");
        if (await uow.ProductoTieneReferencias(r.Id, ct))
            throw new ConflictoException("El producto tiene inventario, proveedores o historial y no se puede eliminar.");
        repo.Eliminar(entidad);
        await uow.Guardar(ct);
        return Unit.Value;
    }
}
