using MediatR;
using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
namespace ImportNet.Application.UseCases.Empleados.Queries;

public sealed record GetEmpleadoByIdQuery(int Id) : IRequest<EmpleadoDto>;
internal sealed class GetEmpleadoByIdQueryHandler(IAdministracionEmpleados empleados, IUsuarioActual usuario) : IRequestHandler<GetEmpleadoByIdQuery, EmpleadoDto>
{
    public async Task<EmpleadoDto> Handle(GetEmpleadoByIdQuery r, CancellationToken ct)
    {
        await usuario.ExigirAdministrador(ct);
        EmpleadoReglas.ValidarId(r.Id);
        return await empleados.Obtener(r.Id, ct) ?? throw new NoEncontradoException("Empleado no encontrado.");
    }
}
