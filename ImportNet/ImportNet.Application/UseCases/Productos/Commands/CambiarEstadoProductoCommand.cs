using MediatR;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
namespace ImportNet.Application.UseCases.Productos.Commands;
public sealed record CambiarEstadoProductoCommand(int Id, bool Activo) : IRequest<Unit>;
internal sealed class CambiarEstadoProductoCommandHandler(IUnitOfWork uow) : IRequestHandler<CambiarEstadoProductoCommand, Unit>
{
    public async Task<Unit> Handle(CambiarEstadoProductoCommand r, CancellationToken ct)
    {
        var entidad = await uow.Productos.Obtener(r.Id, ct) ?? throw new NoEncontradoException("Producto no encontrado.");
        entidad.CambiarEstado(r.Activo);
        await uow.Guardar(ct);
        return Unit.Value;
    }
}
