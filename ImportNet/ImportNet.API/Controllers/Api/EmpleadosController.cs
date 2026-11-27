using ImportNet.Application.Security;
using ImportNet.Application.UseCases.Empleados.Commands;
using ImportNet.Application.UseCases.Empleados.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImportNet.API.Controllers.Api;

[ApiController]
[Route("api/empleados")]
[Authorize(Policy = "Administracion")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class EmpleadosController : ControllerBase
{
    private readonly IMediator _mediator;

    public EmpleadosController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(string? busqueda, bool? activo, CancellationToken ct)
    {
        return Ok(await _mediator.Send(new GetAllEmpleadosQuery(busqueda, activo), ct));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct)
    {
        return Ok(await _mediator.Send(new GetEmpleadoByIdQuery(id), ct));
    }

    [HttpGet("roles")]
    public IActionResult Roles()
    {
        return Ok(RolesImportNet.Todos);
    }

    [HttpPost]
    public async Task<IActionResult> Registrar(CreateEmpleadoCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(Obtener), new { id }, new { id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Editar(int id, UpdateEmpleadoCommand command, CancellationToken ct)
    {
        await _mediator.Send(command with { Id = id }, ct);
        return NoContent();
    }

    [HttpPatch("{id:int}/estado")]
    public async Task<IActionResult> Estado(int id, CambiarEstadoEmpleadoCommand command, CancellationToken ct)
    {
        await _mediator.Send(command with { Id = id }, ct);
        return NoContent();
    }

    [HttpPut("{id:int}/roles")]
    public async Task<IActionResult> AsignarRoles(int id, AsignarRolesEmpleadoCommand command, CancellationToken ct)
    {
        await _mediator.Send(command with { Id = id }, ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        await _mediator.Send(new CambiarEstadoEmpleadoCommand(id, false), ct);
        return NoContent();
    }
}
