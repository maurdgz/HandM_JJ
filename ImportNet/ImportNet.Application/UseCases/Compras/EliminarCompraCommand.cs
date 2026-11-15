using MediatR;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
namespace ImportNet.Application.UseCases.Compras;
public sealed record EliminarCompraCommand(int Id) : IRequest;
internal sealed class EliminarCompraCommandHandler(IUnitOfWork uow) : IRequestHandler<EliminarCompraCommand>
{
    public async Task Handle(EliminarCompraCommand r, CancellationToken ct)
    {
        var c = await uow.Compras.Obtener(r.Id, ct) ?? throw new NoEncontradoException("Compra no encontrada.");
        c.ExigirBorrador(); uow.Compras.Eliminar(c); await uow.Guardar(ct);
    }
}
