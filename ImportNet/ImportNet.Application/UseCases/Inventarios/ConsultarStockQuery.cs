using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Inventarios;

public sealed record ConsultarStockQuery(string? Texto = null, bool StockBajo = false, int Pagina = 1, int Tamano = 20)
    : IRequest<Pagina<StockProducto>>;

internal sealed class ConsultarStockQueryHandler : IRequestHandler<ConsultarStockQuery, Pagina<StockProducto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public ConsultarStockQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<Pagina<StockProducto>> Handle(ConsultarStockQuery request, CancellationToken cancellationToken)
    {
        if (request.Pagina < 1 || request.Pagina > 1000000 || request.Tamano < 1 || request.Tamano > 100)
            throw new ReglaNegocioException("Paginación inválida: página entre 1 y 1000000 y tamaño entre 1 y 100.");

        if (request.Texto != null && request.Texto.Length > 100)
            throw new ReglaNegocioException("La búsqueda admite hasta 100 caracteres.");

        return _unitOfWork.Inventarios.Buscar(request.Texto, request.StockBajo, request.Pagina, request.Tamano, cancellationToken);
    }
}
