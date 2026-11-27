using ImportNet.Application.Security;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Productos.Queries;

public sealed record GetAllProductosQuery : IRequest<IReadOnlyList<ProductoDto>>;

internal sealed class GetAllProductosQueryHandler(IUnitOfWork unitOfWork, IUsuarioActual actor)
    : IRequestHandler<GetAllProductosQuery, IReadOnlyList<ProductoDto>>
{
    public async Task<IReadOnlyList<ProductoDto>> Handle(GetAllProductosQuery request, CancellationToken cancellationToken)
    {
        var esAdministrador = await actor.EsAdministradorAsync(cancellationToken);
        var productos = await unitOfWork.Productos.ListarAsync(cancellationToken);
        return productos.Select(p => ProductoDto.Desde(p, esAdministrador)).ToList();
    }
}
