using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Exceptions;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Ventas;

public sealed record ConsultarVentasQuery(DateOnly? Desde = null, DateOnly? Hasta = null, int Pagina = 1, int Tamano = 20)
    : IRequest<Pagina<ResumenVenta>>;

internal sealed class ConsultarVentasQueryHandler(IUnitOfWork unitOfWork, IUsuarioActual actor)
    : IRequestHandler<ConsultarVentasQuery, Pagina<ResumenVenta>>
{
    public async Task<Pagina<ResumenVenta>> Handle(ConsultarVentasQuery request, CancellationToken cancellationToken)
    {
        if (request.Pagina < 1 || request.Pagina > 1000000 || request.Tamano < 1 || request.Tamano > 100)
            throw new ReglaNegocioException("Paginación inválida.");

        if (request.Desde > request.Hasta)
            throw new ReglaNegocioException("La fecha inicial no puede ser posterior a la final.");

        var empleado = await unitOfWork.Empleados.ObtenerEmpleadoActivoAsync(actor, cancellationToken);
        int? filtroEmpleado = await actor.EsAdministradorAsync(cancellationToken) ? null : empleado.Id;

        DateTime? desdeUtc = request.Desde is not null ? FechasPeru.InicioDelDiaUtc(request.Desde.Value) : null;
        DateTime? hastaUtc = request.Hasta is not null ? FechasPeru.InicioDelDiaUtc(request.Hasta.Value.AddDays(1)) : null;

        return await unitOfWork.Ventas.HistorialAsync(desdeUtc, hastaUtc, filtroEmpleado, request.Pagina, request.Tamano, cancellationToken);
    }
}
