using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using MediatR;

namespace ImportNet.Application.UseCases.Ventas;

public interface IAnulacionVentas
{
    Task Anular(
        int ventaId,
        string motivo,
        int empleadoId,
        CancellationToken ct);
}

public sealed record AnularVentaCommand(
    int Id,
    string Motivo) : IRequest<Unit>;

internal sealed class AnularVentaCommandHandler(
    IAnulacionVentas anulacion,
    IUsuarioActual actor,
    IUsuarioRepository usuarios)
    : IRequestHandler<AnularVentaCommand, Unit>
{
    public async Task<Unit> Handle(
        AnularVentaCommand request,
        CancellationToken ct)
    {
        await actor.ExigirAdministrador(ct);

        if (request.Id <= 0)
        {
            throw new ReglaNegocioException(
                "Identificador inválido.");
        }

        var motivo = Reglas.Texto(
            request.Motivo, 300, "Motivo");

        var usuario = await AccesoVentas.Usuario(
            actor, usuarios, ct);

        await anulacion.Anular(
            request.Id,
            motivo,
            usuario.Empleado.Id,
            ct);

        return Unit.Value;
    }
}