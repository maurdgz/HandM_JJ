using MediatR;
using ImportNet.Application.Security;
namespace ImportNet.Application.UseCases.Empleados.Commands;

public sealed record UpdateEmpleadoCommand(int Id, EmpleadoDatos Datos, bool Activo, string[] Roles) : IRequest<Unit>;
internal sealed class UpdateEmpleadoCommandHandler(IAdministracionEmpleados empleados, IUsuarioActual usuario) : IRequestHandler<UpdateEmpleadoCommand, Unit>
{
    public async Task<Unit> Handle(UpdateEmpleadoCommand r, CancellationToken ct)
    {
        await usuario.ExigirAdministrador(ct);
        EmpleadoReglas.ValidarId(r.Id);
        await empleados.Actualizar(r.Id, EmpleadoReglas.Validar(r.Datos), r.Activo, EmpleadoReglas.ValidarRoles(r.Roles), ct);
        return Unit.Value;
    }
}
