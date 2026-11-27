using ImportNet.Application.Security;
using ImportNet.Domain.Exceptions;
using MediatR;

namespace ImportNet.Application.UseCases.Empleados.Queries;

public sealed record GetAllEmpleadosQuery(string? Busqueda = null, bool? Activo = null) : IRequest<IReadOnlyList<EmpleadoDto>>;

internal sealed class GetAllEmpleadosQueryHandler(IAdministracionEmpleados empleados, IUsuarioActual usuario)
    : IRequestHandler<GetAllEmpleadosQuery, IReadOnlyList<EmpleadoDto>>
{
    public async Task<IReadOnlyList<EmpleadoDto>> Handle(GetAllEmpleadosQuery request, CancellationToken cancellationToken)
    {
        await usuario.ExigirAdministradorAsync(cancellationToken);

        if (request.Busqueda != null && request.Busqueda.Length > 100)
            throw new ReglaNegocioException("La búsqueda admite hasta 100 caracteres.");

        return await empleados.ListarAsync(request.Busqueda?.Trim(), request.Activo, cancellationToken);
    }
}
