using ImportNet.Domain.Entities;
using ImportNet.Domain.Exceptions;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Compras;

public sealed record CrearCompraCommand(CompraDatos Datos) : IRequest<CompraDto>;

internal sealed class CrearCompraCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CrearCompraCommand, CompraDto>
{
    public async Task<CompraDto> Handle(CrearCompraCommand request, CancellationToken cancellationToken)
    {
        var datos = request.Datos ?? throw new ReglaNegocioException("Datos de compra requeridos.");

        var compra = new Compra(datos.ProveedorId, datos.Fecha, datos.TipoCambio, datos.FletePen, datos.AduanaPen, datos.Lineas);
        await datos.ValidarReferenciasAsync(unitOfWork, cancellationToken);

        unitOfWork.Compras.Agregar(compra);
        await unitOfWork.GuardarAsync(cancellationToken);
        return CompraDto.Desde(compra);
    }
}
