using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Productos.Queries;

public sealed record BuscarProductosQuery(string? Texto = null, int? CategoriaId = null, bool StockBajo = false, int Pagina = 1,
    int Tamano = 20, bool? Activo = null) : IRequest<Pagina<Producto>>;

internal sealed class BuscarProductosQueryHandler : IRequestHandler<BuscarProductosQuery, Pagina<Producto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public BuscarProductosQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<Pagina<Producto>> Handle(BuscarProductosQuery request, CancellationToken cancellationToken)
    {
        if (request.Pagina < 1 || request.Pagina > 1000000 || request.Tamano < 1 || request.Tamano > 100)
            throw new ReglaNegocioException("Paginacion invalida (tamano 1-100).");

        return _unitOfWork.Productos.Buscar(request.Texto, request.CategoriaId, request.StockBajo, request.Pagina,
            request.Tamano, request.Activo, cancellationToken);
    }
}
