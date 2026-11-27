using ImportNet.Application.UseCases.Compras;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImportNet.API.Controllers.Api;

[ApiController]
[Route("api/compras")]
[Authorize(Policy = "Administracion")]
public class ComprasController : ControllerBase
{
    private readonly IMediator _mediator;

    public ComprasController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(int pagina = 1, int tamano = 20, CancellationToken ct = default)
    {
        return Ok(await _mediator.Send(new ListarComprasQuery(pagina, tamano), ct));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct)
    {
        return Ok(await _mediator.Send(new ObtenerCompraQuery(id), ct));
    }

    [HttpPost]
    public async Task<IActionResult> Crear(CompraDatos datos, CancellationToken ct)
    {
        var compra = await _mediator.Send(new CrearCompraCommand(datos), ct);
        return CreatedAtAction(nameof(Obtener), new { id = compra.Id }, compra);
    }

    [HttpPost("{id:int}/recibir")]
    public async Task<IActionResult> Recibir(int id, CancellationToken ct)
    {
        return Ok(await _mediator.Send(new RecibirCompraCommand(id), ct));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Editar(int id, CompraDatos datos, CancellationToken ct)
    {
        return Ok(await _mediator.Send(new EditarCompraCommand(id, datos), ct));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        await _mediator.Send(new EliminarCompraCommand(id), ct);
        return NoContent();
    }
}
