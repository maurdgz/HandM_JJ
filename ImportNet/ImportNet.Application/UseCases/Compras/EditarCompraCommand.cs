using ImportNet.Domain.Exceptions;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Compras;

public sealed record EditarCompraCommand(int Id, CompraDatos Datos) : IRequest<CompraDto>;

internal sealed class EditarCompraCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<EditarCompraCommand, CompraDto>
{
    public async Task<CompraDto> Handle(EditarCompraCommand request, CancellationToken cancellationToken)
    {
        var compra = await unitOfWork.Compras.ObtenerConLineasAsync(request.Id, cancellationToken)
            ?? throw new NoEncontradoException("Compra no encontrada.");

        compra.ExigirBorrador();

        var datos = request.Datos ?? throw new ReglaNegocioException("Datos requeridos.");

        await datos.ValidarReferenciasAsync(unitOfWork, cancellationToken);
        compra.Editar(datos.ProveedorId, datos.Fecha, datos.TipoCambio, datos.FletePen, datos.AduanaPen, datos.Lineas);

        unitOfWork.Compras.MarcarEdicion(compra);
        await unitOfWork.GuardarAsync(cancellationToken);
        return CompraDto.Desde(compra);
    }
}
