using ImportNet.Application.Security;
using MediatR;

namespace ImportNet.Application.UseCases.Empleados.Commands;

public sealed record CambiarEstadoEmpleadoCommand(int Id, bool Activo) : IRequest<Unit>;

internal sealed class CambiarEstadoEmpleadoCommandHandler(IAdministracionEmpleados empleados, IUsuarioActual usuario)
    : IRequestHandler<CambiarEstadoEmpleadoCommand, Unit>
{
    public async Task<Unit> Handle(CambiarEstadoEmpleadoCommand request, CancellationToken cancellationToken)
    {
        await usuario.ExigirAdministradorAsync(cancellationToken);
        EmpleadoReglas.ValidarId(request.Id);

        await empleados.CambiarAccesoAsync(request.Id, request.Activo, null, cancellationToken);
        return Unit.Value;
    }
}
