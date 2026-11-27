using ImportNet.Application.Security;
using MediatR;

namespace ImportNet.Application.UseCases.Empleados.Commands;

public sealed record AsignarRolesEmpleadoCommand(int Id, string[] Roles) : IRequest<Unit>;

internal sealed class AsignarRolesEmpleadoCommandHandler : IRequestHandler<AsignarRolesEmpleadoCommand, Unit>
{
    private readonly IAdministracionEmpleados _empleados;
    private readonly IUsuarioActual _usuario;

    public AsignarRolesEmpleadoCommandHandler(IAdministracionEmpleados empleados, IUsuarioActual usuario)
    {
        _empleados = empleados;
        _usuario = usuario;
    }

    public async Task<Unit> Handle(AsignarRolesEmpleadoCommand request, CancellationToken cancellationToken)
    {
        await _usuario.ExigirAdministrador(cancellationToken);
        EmpleadoReglas.ValidarId(request.Id);

        var roles = EmpleadoReglas.ValidarRoles(request.Roles);
        await _empleados.CambiarAcceso(request.Id, null, roles, cancellationToken);
        return Unit.Value;
    }
}
