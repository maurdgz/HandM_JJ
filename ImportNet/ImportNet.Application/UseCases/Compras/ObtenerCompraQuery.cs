using ImportNet.Domain.Exceptions;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Compras;

public sealed record ObtenerCompraQuery(int Id) : IRequest<CompraDto>;

internal sealed class ObtenerCompraQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<ObtenerCompraQuery, CompraDto>
{
    public async Task<CompraDto> Handle(ObtenerCompraQuery request, CancellationToken cancellationToken)
    {
        var compra = await unitOfWork.Compras.ObtenerConLineasAsync(request.Id, cancellationToken)
            ?? throw new NoEncontradoException("Compra no encontrada.");

        return CompraDto.Desde(compra);
    }
}
