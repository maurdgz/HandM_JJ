using MediatR;
using ImportNet.Application.Security;
using ImportNet.Domain.Entities;
namespace ImportNet.Application.UseCases.Empleados.Queries;

public sealed record GetAllEmpleadosQuery(string? Busqueda = null, bool? Activo = null) : IRequest<IReadOnlyList<EmpleadoDto>>;
internal sealed class GetAllEmpleadosQueryHandler(IAdministracionEmpleados empleados, IUsuarioActual usuario) : IRequestHandler<GetAllEmpleadosQuery, IReadOnlyList<EmpleadoDto>>
{
    public async Task<IReadOnlyList<EmpleadoDto>> Handle(GetAllEmpleadosQuery r, CancellationToken ct)
    {
        await usuario.ExigirAdministrador(ct);
        if (r.Busqueda?.Length > 100) throw new ReglaNegocioException("La búsqueda admite hasta 100 caracteres.");
        return await empleados.Listar(r.Busqueda?.Trim(), r.Activo, ct);
    }
}
