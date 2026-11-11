using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ImportNet.Application.UseCases.Productos.Commands;
using ImportNet.Application.UseCases.Productos.Queries;
using ImportNet.Application.UseCases.Productos;
using ImportNet.Application.Security;
namespace ImportNet.API.Controllers.Api;
[ApiController]
[Route("api/productos")]
[Authorize(Policy = "CatalogoLeer")]
public sealed class ProductosController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct)
        => Ok((await mediator.Send(new GetAllProductosQuery(), ct)).Select(p => ProductoDto.Desde(p, User.IsInRole(RolesImportNet.Administrador))));
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct)
        => Ok(ProductoDto.Desde(await mediator.Send(new GetProductoByIdQuery(id), ct), User.IsInRole(RolesImportNet.Administrador)));
    [HttpPost, Authorize(Policy = "CatalogoEditar")]
    public async Task<IActionResult> Crear(CreateProductoCommand command, CancellationToken ct)
    {
        var id = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(Obtener), new { id }, new { id });
    }
    [HttpPut("{id:int}"), Authorize(Policy = "CatalogoEditar")]
    public async Task<IActionResult> Editar(int id, UpdateProductoCommand command, CancellationToken ct)
    {
        await mediator.Send(command with { Id = id }, ct);
        return NoContent();
    }
    [HttpDelete("{id:int}"), Authorize(Policy = "CatalogoEditar")]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        await mediator.Send(new DeleteProductoCommand(id), ct);
        return NoContent();
    }
}
