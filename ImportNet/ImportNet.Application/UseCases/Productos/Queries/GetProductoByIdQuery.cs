using MediatR;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
namespace ImportNet.Application.UseCases.Productos.Queries;
public sealed record GetProductoByIdQuery(int Id) : IRequest<Producto>;
internal sealed class GetProductoByIdQueryHandler(IUnitOfWork uow) : IRequestHandler<GetProductoByIdQuery, Producto>
{
    public async Task<Producto> Handle(GetProductoByIdQuery r, CancellationToken ct)
        => await uow.Productos.Obtener(r.Id, ct) ?? throw new NoEncontradoException("Producto no encontrado.");
}
