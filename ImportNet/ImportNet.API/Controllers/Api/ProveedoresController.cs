using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ImportNet.Application.UseCases.Terceros;
namespace ImportNet.API.Controllers.Api;
[ApiController, Route("api/proveedores"), Authorize(Policy = "Administracion")]
public sealed class ProveedoresController(IMediator mediator) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> Listar(string? texto, CancellationToken ct) => Ok(await mediator.Send(new ListarProveedoresQuery(texto), ct));
    [HttpGet("{id:int}")] public async Task<IActionResult> Obtener(int id, CancellationToken ct) => Ok(await mediator.Send(new ObtenerProveedorQuery(id), ct));
    [HttpPost] public async Task<IActionResult> Crear(GuardarProveedorCommand command, CancellationToken ct)
    { var id = await mediator.Send(command with { Id = 0 }, ct); return Created($"/api/proveedores/{id}", new { id }); }
    [HttpPut("{id:int}"), Authorize(Policy = "Administracion")] public async Task<IActionResult> Editar(int id, GuardarProveedorCommand command, CancellationToken ct)
    { await mediator.Send(command with { Id = id }, ct); return NoContent(); }
    [HttpDelete("{id:int}"), Authorize(Policy = "Administracion")] public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    { await mediator.Send(new EliminarProveedorCommand(id), ct); return NoContent(); }
}
