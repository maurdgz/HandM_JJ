using ImportNet.Application.Security;
using ImportNet.Domain.Exceptions;
using MediatR;

namespace ImportNet.Application.UseCases.Empleados.Commands;

public sealed record CreateEmpleadoCommand(EmpleadoDatos Datos, string Password, bool Activo, string[] Roles) : IRequest<int>;

internal sealed class CreateEmpleadoCommandHandler(IAdministracionEmpleados empleados, IUsuarioActual usuario)
    : IRequestHandler<CreateEmpleadoCommand, int>
{
    public async Task<int> Handle(CreateEmpleadoCommand request, CancellationToken cancellationToken)
    {
        await usuario.ExigirAdministradorAsync(cancellationToken);

        var datos = EmpleadoReglas.Validar(request.Datos);
        var roles = EmpleadoReglas.ValidarRoles(request.Roles);

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 12 || request.Password.Length > 128)
            throw new ReglaNegocioException("La contraseña inicial debe tener entre 12 y 128 caracteres y cumplir la política de Identity.");

        return await empleados.RegistrarAsync(datos, request.Password, request.Activo, roles, cancellationToken);
    }
}
