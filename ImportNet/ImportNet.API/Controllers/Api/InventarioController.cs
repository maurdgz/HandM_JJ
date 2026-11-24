using ImportNet.Application.UseCases.Inventario;
using ImportNet.Domain.Entities;
using ImportNet.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ImportNet.API.Controllers.Api;

[ApiController]
[Route("api/inventario")]
[Authorize(Policy = "CatalogoLeer")]
public sealed class InventarioController(
    IMediator mediator) : ControllerBase
{
    [HttpGet("stock")]
    [ProducesResponseType(
        typeof(Pagina<StockProducto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ConsultarStock(
        [FromQuery] string? texto = null,
        [FromQuery] bool stockBajo = false,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamano = 20,
        CancellationToken ct = default)
    {
        var resultado = await mediator.Send(
            new ConsultarStockQuery(
                texto,
                stockBajo,
                pagina,
                tamano),
            ct);

        return Ok(resultado);
    }
}