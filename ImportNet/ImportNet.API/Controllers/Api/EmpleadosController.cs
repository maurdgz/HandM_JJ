using ImportNet.Application.Security;
using ImportNet.Application.UseCases.Empleados;
using ImportNet.Application.UseCases.Empleados.Commands;
using ImportNet.Application.UseCases.Empleados.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImportNet.API.Controllers.Api;

[ApiController, Route("api/empleados"), Authorize(Policy = "Administracion")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ProducesResponseType(401), ProducesResponseType(403), ProducesResponseType(typeof(ProblemDetails), 400)]
public sealed class EmpleadosController(IMediator mediator) : ControllerBase
{
    [HttpGet, ProducesResponseType(typeof(IReadOnlyList<EmpleadoDto>), 200)]
    public async Task<IActionResult> Listar(string? busqueda, bool? activo, CancellationToken ct)
        => Ok(await mediator.Send(new GetAllEmpleadosQuery(busqueda, activo), ct));
    [HttpGet("{id:int}"), ProducesResponseType(typeof(EmpleadoDto), 200), ProducesResponseType(404)]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct)
        => Ok(await mediator.Send(new GetEmpleadoByIdQuery(id), ct));
    [HttpGet("roles"), ProducesResponseType(typeof(string[]), 200)]
    public IActionResult Roles() => Ok(RolesImportNet.Todos);
    [HttpPost, ProducesResponseType(201), ProducesResponseType(409)]
    public async Task<IActionResult> Registrar(CreateEmpleadoCommand command, CancellationToken ct)
    {
        var id = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(Obtener), new { id }, new { id });
    }
    [HttpPut("{id:int}"), ProducesResponseType(204), ProducesResponseType(404), ProducesResponseType(409)]
    public async Task<IActionResult> Editar(int id, UpdateEmpleadoCommand command, CancellationToken ct)
    {
        await mediator.Send(command with { Id = id }, ct);
        return NoContent();
    }
    [HttpPatch("{id:int}/estado"), ProducesResponseType(204), ProducesResponseType(404), ProducesResponseType(409)]
    public async Task<IActionResult> Estado(int id, CambiarEstadoEmpleadoCommand command, CancellationToken ct)
    {
        await mediator.Send(command with { Id = id }, ct);
        return NoContent();
    }
    [HttpPut("{id:int}/roles"), ProducesResponseType(204), ProducesResponseType(404), ProducesResponseType(409)]
    public async Task<IActionResult> AsignarRoles(int id, AsignarRolesEmpleadoCommand command, CancellationToken ct)
    {
        await mediator.Send(command with { Id = id }, ct);
        return NoContent();
    }
}
