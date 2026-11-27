using ImportNet.Domain.Exceptions;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Compras;

public sealed record ListarComprasQuery(int Pagina = 1, int Tamano = 20) : IRequest<Pagina<CompraDto>>;

internal sealed class ListarComprasQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<ListarComprasQuery, Pagina<CompraDto>>
{
    public async Task<Pagina<CompraDto>> Handle(ListarComprasQuery request, CancellationToken cancellationToken)
    {
        if (request.Pagina < 1 || request.Pagina > 1000000 || request.Tamano < 1 || request.Tamano > 100)
            throw new ReglaNegocioException("Paginacion invalida.");

        var pagina = await unitOfWork.Compras.ListarAsync(request.Pagina, request.Tamano, cancellationToken);
        var items = pagina.Items.Select(CompraDto.Desde).ToList();
        return new Pagina<CompraDto>(items, pagina.Count, pagina.PaginaActual, pagina.Tamano);
    }
}
