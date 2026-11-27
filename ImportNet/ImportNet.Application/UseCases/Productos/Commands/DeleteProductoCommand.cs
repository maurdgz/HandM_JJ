using ImportNet.Domain.Exceptions;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Productos.Commands;

public sealed record DeleteProductoCommand(int Id) : IRequest<Unit>;

internal sealed class DeleteProductoCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteProductoCommand, Unit>
{
    public async Task<Unit> Handle(DeleteProductoCommand request, CancellationToken cancellationToken)
    {
        var producto = await unitOfWork.Productos.ObtenerPorIdAsync(request.Id, cancellationToken)
            ?? throw new NoEncontradoException("Producto no encontrado.");

        if (await unitOfWork.Productos.TieneReferenciasAsync(request.Id, cancellationToken))
            throw new ConflictoException("El producto tiene inventario, proveedores o historial y no se puede eliminar; desactívelo.");

        unitOfWork.Productos.Eliminar(producto);
        await unitOfWork.GuardarAsync(cancellationToken);
        return Unit.Value;
    }
}
