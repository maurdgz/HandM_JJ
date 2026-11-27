using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Productos.Commands;

public sealed record DeleteProductoCommand(int Id) : IRequest<Unit>;

internal sealed class DeleteProductoCommandHandler : IRequestHandler<DeleteProductoCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProductoCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteProductoCommand request, CancellationToken cancellationToken)
    {
        var producto = await _unitOfWork.Productos.Obtener(request.Id, cancellationToken);
        if (producto == null)
            throw new NoEncontradoException("Producto no encontrado.");

        if (await _unitOfWork.Productos.TieneReferencias(request.Id, cancellationToken))
            throw new ConflictoException("El producto tiene inventario, proveedores o historial y no se puede eliminar; desactívelo.");

        _unitOfWork.Productos.Eliminar(producto);
        await _unitOfWork.Guardar(cancellationToken);
        return Unit.Value;
    }
}
