using ImportNet.Application.UseCases.Inventarios;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImportNet.API.Controllers.Api;

[ApiController]
[Route("api/inventario")]
[Authorize(Policy = "CatalogoLeer")]
public sealed class InventarioController(IMediator mediator) : ControllerBase
{
    [HttpGet("stock")]
    [ProducesResponseType(typeof(Pagina<StockProducto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConsultarStock(
        [FromQuery] string? texto = null,
        [FromQuery] bool stockBajo = false,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamano = 20,
        CancellationToken ct = default)
        => Ok(await mediator.Send(new ConsultarStockQuery(texto, stockBajo, pagina, tamano), ct));

    [HttpPost("ajustes"), Authorize(Policy = "Inventario")]
    [ProducesResponseType(typeof(AjusteRegistradoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegistrarAjuste(RegistrarAjusteCommand command, CancellationToken ct)
    {
        var ajuste = await mediator.Send(command, ct);
        return Created($"/api/inventario/kardex/{ajuste.ProductoId}", ajuste);
    }

    [HttpGet("kardex/{productoId:int}"), Authorize(Policy = "Inventario")]
    [ProducesResponseType(typeof(KardexProducto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConsultarKardex(
        int productoId,
        [FromQuery] DateOnly? desde = null,
        [FromQuery] DateOnly? hasta = null,
        CancellationToken ct = default)
        => Ok(await mediator.Send(new ConsultarKardexQuery(productoId, desde, hasta), ct));
}
