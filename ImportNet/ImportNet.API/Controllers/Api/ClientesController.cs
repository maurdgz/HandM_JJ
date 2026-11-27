using ImportNet.Application.UseCases.Terceros;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImportNet.API.Controllers.Api;

[ApiController]
[Route("api/clientes")]
[Authorize(Policy = "CatalogoLeer")]
public class ClientesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ClientesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(string? texto, CancellationToken ct)
    {
        return Ok(await _mediator.Send(new ListarClientesQuery(texto), ct));
    }

    [HttpGet("por-documento/{documento}")]
    public async Task<IActionResult> BuscarPorDocumento(string documento, CancellationToken ct)
    {
        return Ok(await _mediator.Send(new BuscarClientePorDocumentoQuery(documento), ct));
    }

    [HttpPost]
    public async Task<IActionResult> Crear(GuardarClienteCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command with { Id = 0 }, ct);
        return Created($"/api/clientes/{id}", new { id });
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "Administracion")]
    public async Task<IActionResult> Editar(int id, GuardarClienteCommand command, CancellationToken ct)
    {
        await _mediator.Send(command with { Id = id }, ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "Administracion")]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        await _mediator.Send(new EliminarClienteCommand(id), ct);
        return NoContent();
    }
}
