using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Inventario;

/// <summary>Fechas en hora de Perú (UTC−5), ambas inclusivas.</summary>
public sealed record ConsultarKardexQuery(
    int ProductoId,
    DateOnly? Desde = null,
    DateOnly? Hasta = null)
    : IRequest<KardexProducto>;

internal sealed class ConsultarKardexQueryHandler(
    IUnitOfWork uow,
    IUsuarioActual actor)
    : IRequestHandler<ConsultarKardexQuery, KardexProducto>
{
    // Perú no usa horario de verano.
    private static readonly TimeSpan HoraPeru = TimeSpan.FromHours(-5);

    public async Task<KardexProducto> Handle(
        ConsultarKardexQuery request,
        CancellationToken ct)
    {
        await actor.ExigirAdministrador(ct);

        if (request.ProductoId <= 0)
            throw new ReglaNegocioException("Indique un producto válido.");

        if (request.Desde > request.Hasta)
            throw new ReglaNegocioException(
                "La fecha inicial no puede ser posterior a la final.");

        var desdeUtc = request.Desde is { } desde ? InicioDelDiaUtc(desde) : (DateTime?)null;
        var hastaUtc = request.Hasta is { } hasta ? InicioDelDiaUtc(hasta.AddDays(1)) : (DateTime?)null;

        return await uow.Movimientos.Kardex(request.ProductoId, desdeUtc, hastaUtc, ct)
            ?? throw new NoEncontradoException("El producto no existe.");
    }

    private static DateTime InicioDelDiaUtc(DateOnly dia)
        => new DateTimeOffset(dia.ToDateTime(TimeOnly.MinValue), HoraPeru).UtcDateTime;
}
