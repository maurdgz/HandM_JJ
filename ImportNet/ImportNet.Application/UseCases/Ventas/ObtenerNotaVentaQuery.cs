using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Exceptions;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Ventas;

public sealed record ObtenerNotaVentaQuery(int VentaId) : IRequest<NotaVenta>;

internal sealed class ObtenerNotaVentaQueryHandler(IUnitOfWork unitOfWork, IUsuarioActual actor)
    : IRequestHandler<ObtenerNotaVentaQuery, NotaVenta>
{
    public async Task<NotaVenta> Handle(ObtenerNotaVentaQuery request, CancellationToken cancellationToken)
    {
        var empleado = await unitOfWork.Empleados.ObtenerEmpleadoActivoAsync(actor, cancellationToken);

        if (request.VentaId <= 0)
            throw new NoEncontradoException("La venta no existe.");

        var nota = await unitOfWork.Ventas.ObtenerNotaAsync(request.VentaId, cancellationToken)
            ?? throw new NoEncontradoException("La venta no existe.");

        if (nota.EmpleadoId != empleado.Id && !await actor.EsAdministradorAsync(cancellationToken))
            throw new AccesoNoPermitidoException(403);

        return nota;
    }
}
