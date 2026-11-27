using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
using MediatR;

namespace ImportNet.Application.UseCases.Empleados.Queries;

public sealed record GetAllEmpleadosQuery(string? Busqueda = null, bool? Activo = null) : IRequest<IReadOnlyList<EmpleadoDto>>;

internal sealed class GetAllEmpleadosQueryHandler : IRequestHandler<GetAllEmpleadosQuery, IReadOnlyList<EmpleadoDto>>
{
    private readonly IAdministracionEmpleados _empleados;
    private readonly IUsuarioActual _usuario;

    public GetAllEmpleadosQueryHandler(IAdministracionEmpleados empleados, IUsuarioActual usuario)
    {
        _empleados = empleados;
        _usuario = usuario;
    }

    public async Task<IReadOnlyList<EmpleadoDto>> Handle(GetAllEmpleadosQuery request, CancellationToken cancellationToken)
    {
        await _usuario.ExigirAdministrador(cancellationToken);

        if (request.Busqueda != null && request.Busqueda.Length > 100)
            throw new ReglaNegocioException("La búsqueda admite hasta 100 caracteres.");

        return await _empleados.Listar(request.Busqueda?.Trim(), request.Activo, cancellationToken);
    }
}
