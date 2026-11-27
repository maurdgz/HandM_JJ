using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Compras;

public sealed record EditarCompraCommand(int Id, CompraDatos Datos) : IRequest<CompraDto>;

internal sealed class EditarCompraCommandHandler : IRequestHandler<EditarCompraCommand, CompraDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public EditarCompraCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CompraDto> Handle(EditarCompraCommand request, CancellationToken cancellationToken)
    {
        var compra = await _unitOfWork.Compras.Obtener(request.Id, cancellationToken);
        if (compra == null)
            throw new NoEncontradoException("Compra no encontrada.");

        compra.ExigirBorrador();

        var datos = request.Datos;
        if (datos == null)
            throw new ReglaNegocioException("Datos requeridos.");

        await datos.ValidarReferencias(_unitOfWork, cancellationToken);
        compra.Editar(datos.ProveedorId, datos.Fecha, datos.TipoCambio, datos.FletePen, datos.AduanaPen, datos.Lineas);

        _unitOfWork.Compras.MarcarEdicion(compra);
        await _unitOfWork.Guardar(cancellationToken);
        return CompraDto.Desde(compra);
    }
}
