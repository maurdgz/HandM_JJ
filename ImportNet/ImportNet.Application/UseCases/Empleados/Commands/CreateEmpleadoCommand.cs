using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using MediatR;

namespace ImportNet.Application.UseCases.Empleados.Commands;

public sealed record CreateEmpleadoCommand(EmpleadoDatos Datos, string Password, bool Activo, string[] Roles) : IRequest<int>;

internal sealed class CreateEmpleadoCommandHandler : IRequestHandler<CreateEmpleadoCommand, int>
{
    private readonly IAdministracionEmpleados _empleados;
    private readonly IUsuarioActual _usuario;

    public CreateEmpleadoCommandHandler(IAdministracionEmpleados empleados, IUsuarioActual usuario)
    {
        _empleados = empleados;
        _usuario = usuario;
    }

    public async Task<int> Handle(CreateEmpleadoCommand request, CancellationToken cancellationToken)
    {
        await _usuario.ExigirAdministrador(cancellationToken);

        var datos = EmpleadoReglas.Validar(request.Datos);
        var roles = EmpleadoReglas.ValidarRoles(request.Roles);

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 12 || request.Password.Length > 128)
            throw new ReglaNegocioException("La contraseña inicial debe tener entre 12 y 128 caracteres y cumplir la política de Identity.");

        return await _empleados.Registrar(datos, request.Password, request.Activo, roles, cancellationToken);
    }
}
