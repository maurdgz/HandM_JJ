using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Ventas;

public sealed record ObtenerNotaVentaQuery(int VentaId) : IRequest<NotaVenta>;

internal sealed class ObtenerNotaVentaQueryHandler : IRequestHandler<ObtenerNotaVentaQuery, NotaVenta>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUsuarioActual _actor;
    private readonly IEmpleadoRepository _empleados;

    public ObtenerNotaVentaQueryHandler(IUnitOfWork unitOfWork, IUsuarioActual actor, IEmpleadoRepository empleados)
    {
        _unitOfWork = unitOfWork;
        _actor = actor;
        _empleados = empleados;
    }

    public async Task<NotaVenta> Handle(ObtenerNotaVentaQuery request, CancellationToken cancellationToken)
    {
        var empleado = await _empleados.ObtenerEmpleadoActivo(_actor, cancellationToken);

        if (request.VentaId <= 0)
            throw new NoEncontradoException("La venta no existe.");

        var nota = await _unitOfWork.Ventas.Nota(request.VentaId, cancellationToken);
        if (nota == null)
            throw new NoEncontradoException("La venta no existe.");

        if (nota.EmpleadoId != empleado.Id && !await _actor.EsAdministrador(cancellationToken))
            throw new AccesoNoPermitidoException(403);

        return nota;
    }
}
