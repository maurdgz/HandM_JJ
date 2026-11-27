using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Terceros;

public sealed record ListarProveedoresQuery(string? Texto = null) : IRequest<IReadOnlyList<Proveedor>>;

internal sealed class ListarProveedoresQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<ListarProveedoresQuery, IReadOnlyList<Proveedor>>
{
    public Task<IReadOnlyList<Proveedor>> Handle(ListarProveedoresQuery request, CancellationToken cancellationToken)
        => unitOfWork.Proveedores.ListarAsync(request.Texto, cancellationToken);
}
