using ImportNet.Application.Security;
using ImportNet.Domain.Exceptions;
using MediatR;

namespace ImportNet.Application.UseCases.Empleados.Queries;

public sealed record GetEmpleadoByIdQuery(int Id) : IRequest<EmpleadoDto>;

internal sealed class GetEmpleadoByIdQueryHandler(IAdministracionEmpleados empleados, IUsuarioActual usuario)
    : IRequestHandler<GetEmpleadoByIdQuery, EmpleadoDto>
{
    public async Task<EmpleadoDto> Handle(GetEmpleadoByIdQuery request, CancellationToken cancellationToken)
    {
        await usuario.ExigirAdministradorAsync(cancellationToken);
        EmpleadoReglas.ValidarId(request.Id);

        return await empleados.ObtenerAsync(request.Id, cancellationToken)
            ?? throw new NoEncontradoException("Empleado no encontrado.");
    }
}
