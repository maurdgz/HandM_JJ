using MediatR;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
namespace ImportNet.Application.UseCases.Productos.Queries;
public sealed record GetAllProductosQuery() : IRequest<IReadOnlyList<Producto>>;
internal sealed class GetAllProductosQueryHandler(IUnitOfWork uow) : IRequestHandler<GetAllProductosQuery, IReadOnlyList<Producto>>
{
    public async Task<IReadOnlyList<Producto>> Handle(GetAllProductosQuery r, CancellationToken ct)
        => await uow.Repository<Producto>().Listar(ct);
}
