using ImportNet.Application.Security;
using ImportNet.Application.UseCases.Productos;
using ImportNet.Application.UseCases.Productos.Commands;
using ImportNet.Application.UseCases.Productos.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImportNet.API.Controllers.Api;

[ApiController]
[Route("api/productos")]
[Authorize(Policy = "CatalogoLeer")]
public class ProductosController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductosController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private bool EsAdministrador => User.IsInRole(RolesImportNet.Administrador);

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct)
    {
        var productos = await _mediator.Send(new GetAllProductosQuery(), ct);
        return Ok(productos.Select(p => ProductoDto.Desde(p, EsAdministrador)));
    }

    [HttpGet("buscar")]
    public async Task<IActionResult> Buscar([FromQuery] BuscarProductosQuery query, CancellationToken ct)
    {
        if (!EsAdministrador)
            query = query with { Activo = true };

        var pagina = await _mediator.Send(query, ct);
        return Ok(new
        {
            items = pagina.Items.Select(p => ProductoDto.Desde(p, EsAdministrador)),
            pagina.Count,
            pagina.PaginaActual,
            pagina.Tamano
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct)
    {
        var producto = await _mediator.Send(new GetProductoByIdQuery(id), ct);
        return Ok(ProductoDto.Desde(producto, EsAdministrador));
    }

    [HttpPost]
    [Authorize(Policy = "CatalogoEditar")]
    public async Task<IActionResult> Crear(CreateProductoCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(Obtener), new { id }, new { id });
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "CatalogoEditar")]
    public async Task<IActionResult> Editar(int id, UpdateProductoCommand command, CancellationToken ct)
    {
        await _mediator.Send(command with { Id = id }, ct);
        return NoContent();
    }

    [HttpPatch("{id:int}/estado")]
    [Authorize(Policy = "CatalogoEditar")]
    public async Task<IActionResult> Estado(int id, CambiarEstadoProductoCommand command, CancellationToken ct)
    {
        await _mediator.Send(command with { Id = id }, ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "CatalogoEditar")]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteProductoCommand(id), ct);
        return NoContent();
    }
}
