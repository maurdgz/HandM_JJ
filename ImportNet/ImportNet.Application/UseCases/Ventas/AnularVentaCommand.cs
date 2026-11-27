using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;

namespace ImportNet.Application.UseCases.Ventas;

public sealed record AnularVentaCommand(int VentaId, string Motivo) : IRequest<VentaAnuladaDto>;
public sealed record StockRepuestoDto(int ProductoId, decimal Cantidad, decimal StockNuevo);
public sealed record VentaAnuladaDto(int Id, int Numero, string Estado, string Motivo,
    DateTime FechaAnulacion, IReadOnlyList<StockRepuestoDto> Reposiciones);

internal sealed class AnularVentaCommandHandler(
    IAnulacionVentas anulacion, IUsuarioActual actor, IEmpleadoRepository empleados)
    : IRequestHandler<AnularVentaCommand, VentaAnuladaDto>
{
    public async Task<VentaAnuladaDto> Handle(AnularVentaCommand request, CancellationToken ct)
    {
        await actor.ExigirAdministrador(ct);
        var usuarioId = actor.Id ?? throw new AccesoNoPermitidoException(401);
        if (request.VentaId <= 0) throw new NoEncontradoException("La venta no existe.");
        var motivo = Reglas.Texto(request.Motivo, 300, "Motivo");
        var empleado = await empleados.ObtenerPorUsuario(usuarioId, ct);
        if (empleado is not { Activo: true }) throw new AccesoNoPermitidoException(403);
        return await anulacion.Anular(request.VentaId, motivo, empleado.Id, ct);
    }
}
