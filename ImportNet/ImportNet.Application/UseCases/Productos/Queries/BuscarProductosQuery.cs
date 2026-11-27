using ImportNet.Application.Security;
using ImportNet.Domain.Exceptions;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Productos.Queries;

public sealed record BuscarProductosQuery(
    string? Texto = null,
    int? CategoriaId = null,
    bool StockBajo = false,
    int Pagina = 1,
    int Tamano = 20,
    bool? Activo = null) : IRequest<Pagina<ProductoDto>>;

internal sealed class BuscarProductosQueryHandler(IUnitOfWork unitOfWork, IUsuarioActual actor)
    : IRequestHandler<BuscarProductosQuery, Pagina<ProductoDto>>
{
    public async Task<Pagina<ProductoDto>> Handle(BuscarProductosQuery request, CancellationToken cancellationToken)
    {
        if (request.Pagina < 1 || request.Pagina > 1000000 || request.Tamano < 1 || request.Tamano > 100)
            throw new ReglaNegocioException("Paginacion invalida (tamano 1-100).");

        var esAdministrador = await actor.EsAdministradorAsync(cancellationToken);
        var filtroActivo = esAdministrador ? request.Activo : true;

        var pagina = await unitOfWork.Productos.BuscarAsync(
            request.Texto,
            request.CategoriaId,
            request.StockBajo,
            request.Pagina,
            request.Tamano,
            filtroActivo,
            cancellationToken);

        var items = pagina.Items.Select(p => ProductoDto.Desde(p, esAdministrador)).ToList();
        return new Pagina<ProductoDto>(items, pagina.Count, pagina.PaginaActual, pagina.Tamano);
    }
}
