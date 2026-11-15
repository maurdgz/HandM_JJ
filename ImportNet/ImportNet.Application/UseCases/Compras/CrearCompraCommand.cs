using MediatR;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
namespace ImportNet.Application.UseCases.Compras;
public sealed record CrearCompraCommand(CompraDatos Datos) : IRequest<CompraDto>;
internal sealed class CrearCompraCommandHandler(IUnitOfWork uow) : IRequestHandler<CrearCompraCommand, CompraDto>
{
    public async Task<CompraDto> Handle(CrearCompraCommand r, CancellationToken ct)
    {
        if (r.Datos is null) throw new ReglaNegocioException("Datos de compra requeridos.");
        var d = r.Datos;
        var c = new Compra(d.ProveedorId, d.Fecha, d.TipoCambio, d.FletePen, d.AduanaPen, d.Lineas);
        await d.ValidarReferencias(uow, ct);
        uow.Compras.Agregar(c); await uow.Guardar(ct);
        return CompraDto.Desde(c);
    }
}
