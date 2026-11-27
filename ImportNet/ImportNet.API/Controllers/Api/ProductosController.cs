using ImportNet.Application.UseCases.Productos;
using ImportNet.Application.UseCases.Productos.Commands;
using ImportNet.Application.UseCases.Productos.Queries;
using ImportNet.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImportNet.API.Controllers.Api;

[ApiController]
[Route("api/productos")]
[Authorize(Policy = "CatalogoLeer")]
public sealed class ProductosController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProductoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(CancellationToken ct)
        => Ok(await mediator.Send(new GetAllProductosQuery(), ct));

    [HttpGet("buscar")]
    [ProducesResponseType(typeof(Pagina<ProductoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Buscar([FromQuery] BuscarProductosQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct)
        => Ok(await mediator.Send(new GetProductoByIdQuery(id), ct));

    [HttpPost, Authorize(Policy = "CatalogoEditar")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear(CreateProductoCommand command, CancellationToken ct)
    {
        var id = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(Obtener), new { id }, new { id });
    }

    [HttpPut("{id:int}"), Authorize(Policy = "CatalogoEditar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Editar(int id, UpdateProductoCommand command, CancellationToken ct)
    {
        await mediator.Send(command with { Id = id }, ct);
        return NoContent();
    }

    [HttpPatch("{id:int}/estado"), Authorize(Policy = "CatalogoEditar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Estado(int id, CambiarEstadoProductoCommand command, CancellationToken ct)
    {
        await mediator.Send(command with { Id = id }, ct);
        return NoContent();
    }

    [HttpDelete("{id:int}"), Authorize(Policy = "CatalogoEditar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        await mediator.Send(new DeleteProductoCommand(id), ct);
        return NoContent();
    }
}
