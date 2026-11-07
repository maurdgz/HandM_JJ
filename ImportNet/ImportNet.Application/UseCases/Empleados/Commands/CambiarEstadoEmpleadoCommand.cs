using MediatR;
using ImportNet.Application.Security;
namespace ImportNet.Application.UseCases.Empleados.Commands;

public sealed record CambiarEstadoEmpleadoCommand(int Id, bool Activo) : IRequest<Unit>;
internal sealed class CambiarEstadoEmpleadoCommandHandler(IAdministracionEmpleados empleados, IUsuarioActual usuario) : IRequestHandler<CambiarEstadoEmpleadoCommand, Unit>
{
    public async Task<Unit> Handle(CambiarEstadoEmpleadoCommand r, CancellationToken ct)
    {
        await usuario.ExigirAdministrador(ct);
        EmpleadoReglas.ValidarId(r.Id);
        await empleados.CambiarAcceso(r.Id, r.Activo, null, ct);
        return Unit.Value;
    }
}
