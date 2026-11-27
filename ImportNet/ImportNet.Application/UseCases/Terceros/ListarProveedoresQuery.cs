using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Terceros;

public sealed record ListarProveedoresQuery(string? Texto = null) : IRequest<IReadOnlyList<Proveedor>>;

internal sealed class ListarProveedoresQueryHandler : IRequestHandler<ListarProveedoresQuery, IReadOnlyList<Proveedor>>
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarProveedoresQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<IReadOnlyList<Proveedor>> Handle(ListarProveedoresQuery request, CancellationToken cancellationToken)
    {
        return _unitOfWork.Proveedores.Proveedores(request.Texto, cancellationToken);
    }
}
