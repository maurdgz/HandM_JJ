using MediatR;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
namespace ImportNet.Application.UseCases.Productos.Queries;
public sealed record BuscarProductosQuery(string? Texto = null, int? CategoriaId = null, bool StockBajo = false, int Pagina = 1, int Tamano = 20) : IRequest<Pagina<Producto>>;
internal sealed class BuscarProductosQueryHandler(IUnitOfWork uow) : IRequestHandler<BuscarProductosQuery, Pagina<Producto>>
{
    public Task<Pagina<Producto>> Handle(BuscarProductosQuery r, CancellationToken ct)
    {
        if (r.Pagina < 1 || r.Pagina > 1000000 || r.Tamano < 1 || r.Tamano > 100) throw new ReglaNegocioException("Paginacion invalida (tamano 1-100).");
        return uow.Productos.Buscar(r.Texto, r.CategoriaId, r.StockBajo, r.Pagina, r.Tamano, ct);
    }
}
