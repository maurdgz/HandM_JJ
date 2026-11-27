using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using MediatR;

namespace ImportNet.Application.UseCases.Empleados.Queries;

public sealed record GetEmpleadoByIdQuery(int Id) : IRequest<EmpleadoDto>;

internal sealed class GetEmpleadoByIdQueryHandler : IRequestHandler<GetEmpleadoByIdQuery, EmpleadoDto>
{
    private readonly IAdministracionEmpleados _empleados;
    private readonly IUsuarioActual _usuario;

    public GetEmpleadoByIdQueryHandler(IAdministracionEmpleados empleados, IUsuarioActual usuario)
    {
        _empleados = empleados;
        _usuario = usuario;
    }

    public async Task<EmpleadoDto> Handle(GetEmpleadoByIdQuery request, CancellationToken cancellationToken)
    {
        await _usuario.ExigirAdministrador(cancellationToken);
        EmpleadoReglas.ValidarId(request.Id);

        var empleado = await _empleados.Obtener(request.Id, cancellationToken);
        if (empleado == null)
            throw new NoEncontradoException("Empleado no encontrado.");

        return empleado;
    }
}
