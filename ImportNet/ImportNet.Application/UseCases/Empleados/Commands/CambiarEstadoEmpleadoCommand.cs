using ImportNet.Application.Security;
using MediatR;

namespace ImportNet.Application.UseCases.Empleados.Commands;

public sealed record CambiarEstadoEmpleadoCommand(int Id, bool Activo) : IRequest<Unit>;

internal sealed class CambiarEstadoEmpleadoCommandHandler : IRequestHandler<CambiarEstadoEmpleadoCommand, Unit>
{
    private readonly IAdministracionEmpleados _empleados;
    private readonly IUsuarioActual _usuario;

    public CambiarEstadoEmpleadoCommandHandler(IAdministracionEmpleados empleados, IUsuarioActual usuario)
    {
        _empleados = empleados;
        _usuario = usuario;
    }

    public async Task<Unit> Handle(CambiarEstadoEmpleadoCommand request, CancellationToken cancellationToken)
    {
        await _usuario.ExigirAdministrador(cancellationToken);
        EmpleadoReglas.ValidarId(request.Id);

        await _empleados.CambiarAcceso(request.Id, request.Activo, null, cancellationToken);
        return Unit.Value;
    }
}
