using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Terceros;

public sealed record ProveedorDetalle(Proveedor Proveedor, IReadOnlyList<int> Productos);

public sealed record ObtenerProveedorQuery(int Id) : IRequest<ProveedorDetalle>;

internal sealed class ObtenerProveedorQueryHandler : IRequestHandler<ObtenerProveedorQuery, ProveedorDetalle>
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerProveedorQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ProveedorDetalle> Handle(ObtenerProveedorQuery request, CancellationToken cancellationToken)
    {
        var proveedor = await _unitOfWork.Proveedores.Proveedor(request.Id, cancellationToken);
        if (proveedor == null)
            throw new NoEncontradoException("Proveedor no encontrado.");

        var productos = await _unitOfWork.Proveedores.Productos(request.Id, cancellationToken);
        return new ProveedorDetalle(proveedor, productos);
    }
}
