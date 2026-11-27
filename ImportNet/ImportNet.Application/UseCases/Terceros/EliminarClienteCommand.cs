using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Terceros;

public sealed record EliminarClienteCommand(int Id) : IRequest;

internal sealed class EliminarClienteCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<EliminarClienteCommand>
{
    public async Task Handle(EliminarClienteCommand request, CancellationToken cancellationToken)
    {
        await unitOfWork.Clientes.EliminarClienteAsync(request.Id, cancellationToken);
        await unitOfWork.GuardarAsync(cancellationToken);
    }
}
