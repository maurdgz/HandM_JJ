using MediatR;
using ImportNet.Application.Security;
namespace ImportNet.Application.UseCases.Empleados.Commands;

public sealed record AsignarRolesEmpleadoCommand(int Id, string[] Roles) : IRequest<Unit>;
internal sealed class AsignarRolesEmpleadoCommandHandler(IAdministracionEmpleados empleados, IUsuarioActual usuario) : IRequestHandler<AsignarRolesEmpleadoCommand, Unit>
{
    public async Task<Unit> Handle(AsignarRolesEmpleadoCommand r, CancellationToken ct)
    {
        await usuario.ExigirAdministrador(ct);
        EmpleadoReglas.ValidarId(r.Id);
        await empleados.CambiarAcceso(r.Id, null, EmpleadoReglas.ValidarRoles(r.Roles), ct);
        return Unit.Value;
    }
}
