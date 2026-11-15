using MediatR;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
namespace ImportNet.Application.UseCases.Compras;
public sealed record EditarCompraCommand(int Id, CompraDatos Datos) : IRequest<CompraDto>;
internal sealed class EditarCompraCommandHandler(IUnitOfWork uow) : IRequestHandler<EditarCompraCommand, CompraDto>
{
    public async Task<CompraDto> Handle(EditarCompraCommand r, CancellationToken ct)
    {
        var c = await uow.Compras.Obtener(r.Id, ct) ?? throw new NoEncontradoException("Compra no encontrada.");
        c.ExigirBorrador();
        var d = r.Datos ?? throw new ReglaNegocioException("Datos requeridos.");
        await d.ValidarReferencias(uow, ct);
        c.Editar(d.ProveedorId, d.Fecha, d.TipoCambio, d.FletePen, d.AduanaPen, d.Lineas);
        uow.Compras.MarcarEdicion(c); await uow.Guardar(ct);
        return CompraDto.Desde(c);
    }
}
