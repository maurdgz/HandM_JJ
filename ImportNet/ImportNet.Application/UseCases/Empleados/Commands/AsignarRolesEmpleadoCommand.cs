using ImportNet.Application.Security;
using MediatR;

namespace ImportNet.Application.UseCases.Empleados.Commands;

public sealed record AsignarRolesEmpleadoCommand(int Id, string[] Roles) : IRequest<Unit>;

internal sealed class AsignarRolesEmpleadoCommandHandler(IAdministracionEmpleados empleados, IUsuarioActual usuario)
    : IRequestHandler<AsignarRolesEmpleadoCommand, Unit>
{
    public async Task<Unit> Handle(AsignarRolesEmpleadoCommand request, CancellationToken cancellationToken)
    {
        await usuario.ExigirAdministradorAsync(cancellationToken);
        EmpleadoReglas.ValidarId(request.Id);

        var roles = EmpleadoReglas.ValidarRoles(request.Roles);
        await empleados.CambiarAccesoAsync(request.Id, null, roles, cancellationToken);
        return Unit.Value;
    }
}
