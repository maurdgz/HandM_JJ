using ImportNet.Application.Security;
using MediatR;

namespace ImportNet.Application.UseCases.Empleados.Commands;

public sealed record UpdateEmpleadoCommand(int Id, EmpleadoDatos Datos, bool Activo, string[] Roles) : IRequest<Unit>;

internal sealed class UpdateEmpleadoCommandHandler : IRequestHandler<UpdateEmpleadoCommand, Unit>
{
    private readonly IAdministracionEmpleados _empleados;
    private readonly IUsuarioActual _usuario;

    public UpdateEmpleadoCommandHandler(IAdministracionEmpleados empleados, IUsuarioActual usuario)
    {
        _empleados = empleados;
        _usuario = usuario;
    }

    public async Task<Unit> Handle(UpdateEmpleadoCommand request, CancellationToken cancellationToken)
    {
        await _usuario.ExigirAdministrador(cancellationToken);
        EmpleadoReglas.ValidarId(request.Id);

        var datos = EmpleadoReglas.Validar(request.Datos);
        var roles = EmpleadoReglas.ValidarRoles(request.Roles);

        await _empleados.Actualizar(request.Id, datos, request.Activo, roles, cancellationToken);
        return Unit.Value;
    }
}
