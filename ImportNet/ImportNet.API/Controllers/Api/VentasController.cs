using ImportNet.Application.UseCases.Ventas;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImportNet.API.Controllers.Api;

[ApiController]
[Route("api/ventas")]
[Authorize(Policy = "Ventas")]
public class VentasController : ControllerBase
{
    private readonly IMediator _mediator;

    public VentasController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Registrar(RegistrarVentaCommand command, CancellationToken ct)
    {
        var venta = await _mediator.Send(command, ct);
        return Created($"/api/ventas/{venta.Id}", venta);
    }

    [HttpGet]
    public async Task<IActionResult> Historial(DateOnly? desde = null, DateOnly? hasta = null, int pagina = 1, int tamano = 20,
        CancellationToken ct = default)
    {
        return Ok(await _mediator.Send(new ConsultarVentasQuery(desde, hasta, pagina, tamano), ct));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Nota(int id, CancellationToken ct)
    {
        return Ok(await _mediator.Send(new ObtenerNotaVentaQuery(id), ct));
    }

    [HttpPost("{id:int}/anulacion")]
    [Authorize(Policy = "Administracion")]
    public async Task<IActionResult> Anular(int id, AnularVentaBody body, CancellationToken ct)
    {
        return Ok(await _mediator.Send(new AnularVentaCommand(id, body.Motivo), ct));
    }
}

public sealed record AnularVentaBody(string Motivo);
