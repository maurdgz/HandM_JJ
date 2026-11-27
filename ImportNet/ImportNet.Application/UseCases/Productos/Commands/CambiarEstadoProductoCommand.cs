using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Productos.Commands;

public sealed record CambiarEstadoProductoCommand(int Id, bool Activo) : IRequest<Unit>;

internal sealed class CambiarEstadoProductoCommandHandler : IRequestHandler<CambiarEstadoProductoCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;

    public CambiarEstadoProductoCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(CambiarEstadoProductoCommand request, CancellationToken cancellationToken)
    {
        var producto = await _unitOfWork.Productos.Obtener(request.Id, cancellationToken);
        if (producto == null)
            throw new NoEncontradoException("Producto no encontrado.");

        producto.CambiarEstado(request.Activo);
        await _unitOfWork.Guardar(cancellationToken);
        return Unit.Value;
    }
}
