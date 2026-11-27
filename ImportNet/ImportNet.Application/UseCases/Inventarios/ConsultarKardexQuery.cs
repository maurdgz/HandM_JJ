using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Inventarios;

public sealed record ConsultarKardexQuery(int ProductoId, DateOnly? Desde = null, DateOnly? Hasta = null) : IRequest<KardexProducto>;

internal sealed class ConsultarKardexQueryHandler : IRequestHandler<ConsultarKardexQuery, KardexProducto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUsuarioActual _actor;

    public ConsultarKardexQueryHandler(IUnitOfWork unitOfWork, IUsuarioActual actor)
    {
        _unitOfWork = unitOfWork;
        _actor = actor;
    }

    public async Task<KardexProducto> Handle(ConsultarKardexQuery request, CancellationToken cancellationToken)
    {
        await _actor.ExigirAdministrador(cancellationToken);

        if (request.ProductoId <= 0)
            throw new ReglaNegocioException("Indique un producto válido.");

        if (request.Desde > request.Hasta)
            throw new ReglaNegocioException("La fecha inicial no puede ser posterior a la final.");

        DateTime? desdeUtc = null;
        if (request.Desde != null)
            desdeUtc = FechasPeru.InicioDelDiaUtc(request.Desde.Value);

        DateTime? hastaUtc = null;
        if (request.Hasta != null)
            hastaUtc = FechasPeru.InicioDelDiaUtc(request.Hasta.Value.AddDays(1));

        var kardex = await _unitOfWork.Movimientos.Kardex(request.ProductoId, desdeUtc, hastaUtc, cancellationToken);
        if (kardex == null)
            throw new NoEncontradoException("El producto no existe.");

        return kardex;
    }
}
