using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ImportNet.Application.UseCases.Terceros;
namespace ImportNet.API.Controllers.Api;
[ApiController, Route("api/clientes"), Authorize(Policy = "CatalogoLeer")]
public sealed class ClientesController(IMediator mediator) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> Listar(string? texto, CancellationToken ct) => Ok(await mediator.Send(new ListarClientesQuery(texto), ct));
    [HttpGet("por-documento/{documento}")]
    public async Task<IActionResult> BuscarPorDocumento(string documento, CancellationToken ct)
        => Ok(await mediator.Send(new BuscarClientePorDocumentoQuery(documento), ct));

    [HttpPost] public async Task<IActionResult> Crear(GuardarClienteCommand command, CancellationToken ct)
    { var id = await mediator.Send(command with { Id = 0 }, ct); return Created($"/api/clientes/{id}", new { id }); }
    [HttpPut("{id:int}"), Authorize(Policy = "Administracion")] public async Task<IActionResult> Editar(int id, GuardarClienteCommand command, CancellationToken ct)
    { await mediator.Send(command with { Id = id }, ct); return NoContent(); }
    [HttpDelete("{id:int}"), Authorize(Policy = "Administracion")] public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    { await mediator.Send(new EliminarClienteCommand(id), ct); return NoContent(); }
}
