using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Terceros;

public sealed record EliminarProveedorCommand(int Id) : IRequest;

internal sealed class EliminarProveedorCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<EliminarProveedorCommand>
{
    public async Task Handle(EliminarProveedorCommand request, CancellationToken cancellationToken)
    {
        await unitOfWork.Proveedores.EliminarProveedorAsync(request.Id, cancellationToken);
        await unitOfWork.GuardarAsync(cancellationToken);
    }
}
