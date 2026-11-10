using MediatR;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
namespace ImportNet.Application.UseCases.Productos.Commands;
public sealed record DeleteProductoCommand(int Id) : IRequest<Unit>;
internal sealed class DeleteProductoCommandHandler(IUnitOfWork uow) : IRequestHandler<DeleteProductoCommand, Unit>
{
    public async Task<Unit> Handle(DeleteProductoCommand r, CancellationToken ct)
    {
        var entidad = await uow.Productos.Obtener(r.Id, ct) ?? throw new NoEncontradoException("Producto no encontrado.");
        if (await uow.Productos.TieneReferencias(r.Id, ct))
            throw new ConflictoException("El producto tiene inventario, proveedores o historial y no se puede eliminar.");
        uow.Productos.Eliminar(entidad);
        await uow.Guardar(ct);
        return Unit.Value;
    }
}
