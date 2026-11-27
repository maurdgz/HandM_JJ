using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Terceros;

public sealed record ListarClientesQuery(string? Texto = null) : IRequest<IReadOnlyList<Cliente>>;

internal sealed class ListarClientesQueryHandler : IRequestHandler<ListarClientesQuery, IReadOnlyList<Cliente>>
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarClientesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<IReadOnlyList<Cliente>> Handle(ListarClientesQuery request, CancellationToken cancellationToken)
    {
        return _unitOfWork.Clientes.Clientes(request.Texto, cancellationToken);
    }
}
