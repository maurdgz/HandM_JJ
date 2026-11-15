using MediatR;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
namespace ImportNet.Application.UseCases.Compras;
public sealed record ObtenerCompraQuery(int Id) : IRequest<CompraDto>;
internal sealed class ObtenerCompraQueryHandler(IUnitOfWork uow) : IRequestHandler<ObtenerCompraQuery, CompraDto>
{
    public async Task<CompraDto> Handle(ObtenerCompraQuery r, CancellationToken ct) => CompraDto.Desde(await uow.Compras.Obtener(r.Id, ct) ?? throw new NoEncontradoException("Compra no encontrada."));
}
