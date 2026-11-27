using ImportNet.Application.Security;
using ImportNet.Domain.Exceptions;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Productos.Queries;

public sealed record GetProductoByIdQuery(int Id) : IRequest<ProductoDto>;

internal sealed class GetProductoByIdQueryHandler(IUnitOfWork unitOfWork, IUsuarioActual actor)
    : IRequestHandler<GetProductoByIdQuery, ProductoDto>
{
    public async Task<ProductoDto> Handle(GetProductoByIdQuery request, CancellationToken cancellationToken)
    {
        var producto = await unitOfWork.Productos.ObtenerPorIdAsync(request.Id, cancellationToken)
            ?? throw new NoEncontradoException("Producto no encontrado.");

        var esAdministrador = await actor.EsAdministradorAsync(cancellationToken);
        return ProductoDto.Desde(producto, esAdministrador);
    }
}
