using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Ventas;

public sealed record ConsultarVentasQuery(DateOnly? Desde = null, DateOnly? Hasta = null, int Pagina = 1, int Tamano = 20)
    : IRequest<Pagina<ResumenVenta>>;

internal sealed class ConsultarVentasQueryHandler : IRequestHandler<ConsultarVentasQuery, Pagina<ResumenVenta>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUsuarioActual _actor;
    private readonly IEmpleadoRepository _empleados;

    public ConsultarVentasQueryHandler(IUnitOfWork unitOfWork, IUsuarioActual actor, IEmpleadoRepository empleados)
    {
        _unitOfWork = unitOfWork;
        _actor = actor;
        _empleados = empleados;
    }

    public async Task<Pagina<ResumenVenta>> Handle(ConsultarVentasQuery request, CancellationToken cancellationToken)
    {
        if (request.Pagina < 1 || request.Pagina > 1000000 || request.Tamano < 1 || request.Tamano > 100)
            throw new ReglaNegocioException("Paginación inválida.");

        if (request.Desde > request.Hasta)
            throw new ReglaNegocioException("La fecha inicial no puede ser posterior a la final.");

        var empleado = await _empleados.ObtenerEmpleadoActivo(_actor, cancellationToken);

        int? filtroEmpleado = empleado.Id;
        if (await _actor.EsAdministrador(cancellationToken))
            filtroEmpleado = null;

        DateTime? desdeUtc = null;
        if (request.Desde != null)
            desdeUtc = FechasPeru.InicioDelDiaUtc(request.Desde.Value);

        DateTime? hastaUtc = null;
        if (request.Hasta != null)
            hastaUtc = FechasPeru.InicioDelDiaUtc(request.Hasta.Value.AddDays(1));

        return await _unitOfWork.Ventas.Historial(desdeUtc, hastaUtc, filtroEmpleado, request.Pagina, request.Tamano, cancellationToken);
    }
}
