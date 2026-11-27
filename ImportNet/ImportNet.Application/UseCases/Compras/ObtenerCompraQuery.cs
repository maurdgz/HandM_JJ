using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Compras;

public sealed record ObtenerCompraQuery(int Id) : IRequest<CompraDto>;

internal sealed class ObtenerCompraQueryHandler : IRequestHandler<ObtenerCompraQuery, CompraDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerCompraQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CompraDto> Handle(ObtenerCompraQuery request, CancellationToken cancellationToken)
    {
        var compra = await _unitOfWork.Compras.Obtener(request.Id, cancellationToken);
        if (compra == null)
            throw new NoEncontradoException("Compra no encontrada.");

        return CompraDto.Desde(compra);
    }
}
