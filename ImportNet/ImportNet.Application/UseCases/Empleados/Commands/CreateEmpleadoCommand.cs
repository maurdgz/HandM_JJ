using MediatR;
using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
namespace ImportNet.Application.UseCases.Empleados.Commands;

public sealed record CreateEmpleadoCommand(EmpleadoDatos Datos, string Password, bool Activo, string[] Roles) : IRequest<int>;
internal sealed class CreateEmpleadoCommandHandler(IAdministracionEmpleados empleados, IUsuarioActual usuario) : IRequestHandler<CreateEmpleadoCommand, int>
{
    public async Task<int> Handle(CreateEmpleadoCommand r, CancellationToken ct)
    {
        await usuario.ExigirAdministrador(ct);
        var datos = EmpleadoReglas.Validar(r.Datos);
        var roles = EmpleadoReglas.ValidarRoles(r.Roles);
        if (string.IsNullOrWhiteSpace(r.Password) || r.Password.Length is < 12 or > 128)
            throw new ReglaNegocioException("La contraseña inicial debe tener entre 12 y 128 caracteres y cumplir la política de Identity.");
        return await empleados.Registrar(datos, r.Password, r.Activo, roles, ct);
    }
}
