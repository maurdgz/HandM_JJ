using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Terceros;

public sealed record ListarClientesQuery(string? Texto = null) : IRequest<IReadOnlyList<Cliente>>;

internal sealed class ListarClientesQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<ListarClientesQuery, IReadOnlyList<Cliente>>
{
    public Task<IReadOnlyList<Cliente>> Handle(ListarClientesQuery request, CancellationToken cancellationToken)
        => unitOfWork.Clientes.ListarAsync(request.Texto, cancellationToken);
}
