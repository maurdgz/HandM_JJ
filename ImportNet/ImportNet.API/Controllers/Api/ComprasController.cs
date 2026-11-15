using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ImportNet.Application.UseCases.Compras;
namespace ImportNet.API.Controllers.Api;
[ApiController, Route("api/compras"), Authorize(Policy = "Administracion")]
public sealed class ComprasController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] int pagina = 1, [FromQuery] int tamano = 20, CancellationToken ct = default)
        => Ok(await mediator.Send(new ListarComprasQuery(pagina, tamano), ct));
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct) => Ok(await mediator.Send(new ObtenerCompraQuery(id), ct));
    [HttpPost]
    public async Task<IActionResult> Crear(CompraDatos datos, CancellationToken ct)
    {
        var compra = await mediator.Send(new CrearCompraCommand(datos), ct);
        return CreatedAtAction(nameof(Obtener), new { id = compra.Id }, compra);
    }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Editar(int id, CompraDatos datos, CancellationToken ct)
        => Ok(await mediator.Send(new EditarCompraCommand(id, datos), ct));
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        await mediator.Send(new EliminarCompraCommand(id), ct); return NoContent();
    }
}
