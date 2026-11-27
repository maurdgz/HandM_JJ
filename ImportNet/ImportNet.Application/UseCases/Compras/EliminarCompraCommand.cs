using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Compras;

public sealed record EliminarCompraCommand(int Id) : IRequest;

internal sealed class EliminarCompraCommandHandler : IRequestHandler<EliminarCompraCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public EliminarCompraCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(EliminarCompraCommand request, CancellationToken cancellationToken)
    {
        var compra = await _unitOfWork.Compras.Obtener(request.Id, cancellationToken);
        if (compra == null)
            throw new NoEncontradoException("Compra no encontrada.");

        _unitOfWork.Compras.Eliminar(compra);
        await _unitOfWork.Guardar(cancellationToken);
    }
}
