using ImportNet.Application.UseCases.Ventas;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImportNet.API.Controllers.Api;

[ApiController, Route("api/ventas"), Authorize(Policy = "Ventas")]
public sealed class VentasController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Registrar(RegistrarVentaCommand command, CancellationToken ct)
    {
        var venta = await mediator.Send(command, ct);
        return Created($"/api/ventas/{venta.Id}", venta);
    }
}
