using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Exceptions;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Inventarios;

public sealed record ConsultarKardexQuery(int ProductoId, DateOnly? Desde = null, DateOnly? Hasta = null)
    : IRequest<KardexProducto>;

internal sealed class ConsultarKardexQueryHandler(IUnitOfWork unitOfWork, IUsuarioActual actor)
    : IRequestHandler<ConsultarKardexQuery, KardexProducto>
{
    public async Task<KardexProducto> Handle(ConsultarKardexQuery request, CancellationToken cancellationToken)
    {
        await actor.ExigirAdministradorAsync(cancellationToken);

        if (request.ProductoId <= 0)
            throw new ReglaNegocioException("Indique un producto válido.");

        if (request.Desde > request.Hasta)
            throw new ReglaNegocioException("La fecha inicial no puede ser posterior a la final.");

        DateTime? desdeUtc = request.Desde is not null ? FechasPeru.InicioDelDiaUtc(request.Desde.Value) : null;
        DateTime? hastaUtc = request.Hasta is not null ? FechasPeru.InicioDelDiaUtc(request.Hasta.Value.AddDays(1)) : null;

        return await unitOfWork.Movimientos.ObtenerKardexAsync(request.ProductoId, desdeUtc, hastaUtc, cancellationToken)
            ?? throw new NoEncontradoException("El producto no existe.");
    }
}
