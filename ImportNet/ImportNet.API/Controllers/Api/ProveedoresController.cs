using ImportNet.Application.UseCases.Terceros;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImportNet.API.Controllers.Api;

[ApiController]
[Route("api/proveedores")]
[Authorize(Policy = "Administracion")]
public class ProveedoresController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProveedoresController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(string? texto, CancellationToken ct)
    {
        return Ok(await _mediator.Send(new ListarProveedoresQuery(texto), ct));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct)
    {
        return Ok(await _mediator.Send(new ObtenerProveedorQuery(id), ct));
    }

    [HttpPost]
    public async Task<IActionResult> Crear(GuardarProveedorCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command with { Id = 0 }, ct);
        return Created($"/api/proveedores/{id}", new { id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Editar(int id, GuardarProveedorCommand command, CancellationToken ct)
    {
        await _mediator.Send(command with { Id = id }, ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        await _mediator.Send(new EliminarProveedorCommand(id), ct);
        return NoContent();
    }
}
