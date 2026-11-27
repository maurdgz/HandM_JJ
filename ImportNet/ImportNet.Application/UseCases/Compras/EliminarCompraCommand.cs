using ImportNet.Domain.Exceptions;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Compras;

public sealed record EliminarCompraCommand(int Id) : IRequest;

internal sealed class EliminarCompraCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<EliminarCompraCommand>
{
    public async Task Handle(EliminarCompraCommand request, CancellationToken cancellationToken)
    {
        var compra = await unitOfWork.Compras.ObtenerConLineasAsync(request.Id, cancellationToken)
            ?? throw new NoEncontradoException("Compra no encontrada.");

        unitOfWork.Compras.Eliminar(compra);
        await unitOfWork.GuardarAsync(cancellationToken);
    }
}
