using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Terceros;

public sealed record EliminarClienteCommand(int Id) : IRequest;

internal sealed class EliminarClienteCommandHandler : IRequestHandler<EliminarClienteCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public EliminarClienteCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(EliminarClienteCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.Clientes.EliminarCliente(request.Id, cancellationToken);
        await _unitOfWork.Guardar(cancellationToken);
    }
}
