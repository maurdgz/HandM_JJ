using ImportNet.Application.UseCases.Terceros;
using ImportNet.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImportNet.API.Controllers.Api;

[ApiController]
[Route("api/proveedores")]
[Authorize(Policy = "Administracion")]
public sealed class ProveedoresController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<Proveedor>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(string? texto, CancellationToken ct)
        => Ok(await mediator.Send(new ListarProveedoresQuery(texto), ct));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProveedorDetalle), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct)
        => Ok(await mediator.Send(new ObtenerProveedorQuery(id), ct));

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Crear(GuardarProveedorCommand command, CancellationToken ct)
    {
        var id = await mediator.Send(command with { Id = 0 }, ct);
        return Created($"/api/proveedores/{id}", new { id });
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Editar(int id, GuardarProveedorCommand command, CancellationToken ct)
    {
        await mediator.Send(command with { Id = id }, ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        await mediator.Send(new EliminarProveedorCommand(id), ct);
        return NoContent();
    }
}
