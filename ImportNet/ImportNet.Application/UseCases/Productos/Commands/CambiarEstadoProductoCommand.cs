using ImportNet.Domain.Exceptions;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Productos.Commands;

public sealed record CambiarEstadoProductoCommand(int Id, bool Activo) : IRequest<Unit>;

internal sealed class CambiarEstadoProductoCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CambiarEstadoProductoCommand, Unit>
{
    public async Task<Unit> Handle(CambiarEstadoProductoCommand request, CancellationToken cancellationToken)
    {
        var producto = await unitOfWork.Productos.ObtenerPorIdAsync(request.Id, cancellationToken)
            ?? throw new NoEncontradoException("Producto no encontrado.");

        producto.CambiarEstado(request.Activo);
        await unitOfWork.GuardarAsync(cancellationToken);
        return Unit.Value;
    }
}
