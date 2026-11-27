using ImportNet.Domain.Entities;
using ImportNet.Domain.Exceptions;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Terceros;

public sealed record ProveedorDetalle(Proveedor Proveedor, IReadOnlyList<int> Productos);

public sealed record ObtenerProveedorQuery(int Id) : IRequest<ProveedorDetalle>;

internal sealed class ObtenerProveedorQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<ObtenerProveedorQuery, ProveedorDetalle>
{
    public async Task<ProveedorDetalle> Handle(ObtenerProveedorQuery request, CancellationToken cancellationToken)
    {
        var proveedor = await unitOfWork.Proveedores.ObtenerPorIdAsync(request.Id, cancellationToken)
            ?? throw new NoEncontradoException("Proveedor no encontrado.");

        var productos = await unitOfWork.Proveedores.ObtenerProductosIdsAsync(request.Id, cancellationToken);
        return new ProveedorDetalle(proveedor, productos);
    }
}
