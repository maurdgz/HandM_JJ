using ImportNet.Application.UseCases.Compras;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ImportNet.API.Controllers.Api;

[ApiController]
[Route("api/compras")]
[Authorize(Policy = "Administracion")]
public sealed class ComprasController(
    IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamano = 20,
        CancellationToken ct = default)
    {
        return Ok(
            await mediator.Send(
                new ListarComprasQuery(pagina, tamano),
                ct));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(
        int id,
        CancellationToken ct)
    {
        return Ok(
            await mediator.Send(
                new ObtenerCompraQuery(id),
                ct));
    }

    [HttpPost]
    public async Task<IActionResult> Crear(
        CompraDatos datos,
        CancellationToken ct)
    {
        var compra = await mediator.Send(
            new CrearCompraCommand(datos),
            ct);

        return CreatedAtAction(
            nameof(Obtener),
            new { id = compra.Id },
            compra);
    }

    [HttpPost("{id:int}/recibir")]
    [ProducesResponseType(
        typeof(CompraDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Recibir(
        [FromRoute] int id,
        CancellationToken ct)
    {
        var compra = await mediator.Send(
            new RecibirCompraCommand(id),
            ct);

        return Ok(compra);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Editar(
        int id,
        CompraDatos datos,
        CancellationToken ct)
    {
        return Ok(
            await mediator.Send(
                new EditarCompraCommand(id, datos),
                ct));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(
        int id,
        CancellationToken ct)
    {
        await mediator.Send(
            new EliminarCompraCommand(id),
            ct);

        return NoContent();
    }
}