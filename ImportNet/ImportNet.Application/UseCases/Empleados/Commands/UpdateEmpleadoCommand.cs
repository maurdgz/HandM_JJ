using ImportNet.Application.Security;
using MediatR;

namespace ImportNet.Application.UseCases.Empleados.Commands;

public sealed record UpdateEmpleadoCommand(int Id, EmpleadoDatos Datos, bool Activo, string[] Roles) : IRequest<Unit>;

internal sealed class UpdateEmpleadoCommandHandler(IAdministracionEmpleados empleados, IUsuarioActual usuario)
    : IRequestHandler<UpdateEmpleadoCommand, Unit>
{
    public async Task<Unit> Handle(UpdateEmpleadoCommand request, CancellationToken cancellationToken)
    {
        await usuario.ExigirAdministradorAsync(cancellationToken);
        EmpleadoReglas.ValidarId(request.Id);

        var datos = EmpleadoReglas.Validar(request.Datos);
        var roles = EmpleadoReglas.ValidarRoles(request.Roles);

        await empleados.ActualizarAsync(request.Id, datos, request.Activo, roles, cancellationToken);
        return Unit.Value;
    }
}
