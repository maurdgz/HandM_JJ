using MediatR;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
namespace ImportNet.Application.UseCases.Compras;
public sealed record ListarComprasQuery(int Pagina = 1, int Tamano = 20) : IRequest<Pagina<CompraDto>>;
internal sealed class ListarComprasQueryHandler(IUnitOfWork uow) : IRequestHandler<ListarComprasQuery, Pagina<CompraDto>>
{
    public async Task<Pagina<CompraDto>> Handle(ListarComprasQuery r, CancellationToken ct)
    {
        if (r.Pagina < 1 || r.Pagina > 1000000 || r.Tamano < 1 || r.Tamano > 100) throw new ReglaNegocioException("Paginacion invalida.");
        var p = await uow.Compras.Listar(r.Pagina, r.Tamano, ct);
        return new(p.Items.Select(CompraDto.Desde).ToArray(), p.Count, p.PaginaActual, p.Tamano);
    }
}
