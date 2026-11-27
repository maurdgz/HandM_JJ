using ImportNet.Application.UseCases.Inventarios;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImportNet.API.Controllers.Api;

[ApiController]
[Route("api/inventario")]
[Authorize(Policy = "CatalogoLeer")]
public class InventarioController : ControllerBase
{
    private readonly IMediator _mediator;

    public InventarioController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("stock")]
    public async Task<IActionResult> ConsultarStock(string? texto = null, bool stockBajo = false, int pagina = 1, int tamano = 20,
        CancellationToken ct = default)
    {
        return Ok(await _mediator.Send(new ConsultarStockQuery(texto, stockBajo, pagina, tamano), ct));
    }

    [HttpPost("ajustes")]
    [Authorize(Policy = "Inventario")]
    public async Task<IActionResult> RegistrarAjuste(RegistrarAjusteCommand command, CancellationToken ct)
    {
        var ajuste = await _mediator.Send(command, ct);
        return Created($"/api/inventario/kardex/{ajuste.ProductoId}", ajuste);
    }

    [HttpGet("kardex/{productoId:int}")]
    [Authorize(Policy = "Inventario")]
    public async Task<IActionResult> ConsultarKardex(int productoId, DateOnly? desde = null, DateOnly? hasta = null,
        CancellationToken ct = default)
    {
        return Ok(await _mediator.Send(new ConsultarKardexQuery(productoId, desde, hasta), ct));
    }
}
