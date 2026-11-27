using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Productos.Queries;

public sealed record GetProductoByIdQuery(int Id) : IRequest<Producto>;

internal sealed class GetProductoByIdQueryHandler : IRequestHandler<GetProductoByIdQuery, Producto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetProductoByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Producto> Handle(GetProductoByIdQuery request, CancellationToken cancellationToken)
    {
        var producto = await _unitOfWork.Productos.Obtener(request.Id, cancellationToken);
        if (producto == null)
            throw new NoEncontradoException("Producto no encontrado.");

        return producto;
    }
}
