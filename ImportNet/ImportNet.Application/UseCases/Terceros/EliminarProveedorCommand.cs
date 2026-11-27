using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Terceros;

public sealed record EliminarProveedorCommand(int Id) : IRequest;

internal sealed class EliminarProveedorCommandHandler : IRequestHandler<EliminarProveedorCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public EliminarProveedorCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(EliminarProveedorCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.Proveedores.EliminarProveedor(request.Id, cancellationToken);
        await _unitOfWork.Guardar(cancellationToken);
    }
}
