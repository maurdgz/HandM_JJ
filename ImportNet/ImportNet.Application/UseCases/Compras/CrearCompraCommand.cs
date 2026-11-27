using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Compras;

public sealed record CrearCompraCommand(CompraDatos Datos) : IRequest<CompraDto>;

internal sealed class CrearCompraCommandHandler : IRequestHandler<CrearCompraCommand, CompraDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearCompraCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CompraDto> Handle(CrearCompraCommand request, CancellationToken cancellationToken)
    {
        var datos = request.Datos;
        if (datos == null)
            throw new ReglaNegocioException("Datos de compra requeridos.");

        var compra = new Compra(datos.ProveedorId, datos.Fecha, datos.TipoCambio, datos.FletePen, datos.AduanaPen, datos.Lineas);
        await datos.ValidarReferencias(_unitOfWork, cancellationToken);

        _unitOfWork.Compras.Agregar(compra);
        await _unitOfWork.Guardar(cancellationToken);
        return CompraDto.Desde(compra);
    }
}
