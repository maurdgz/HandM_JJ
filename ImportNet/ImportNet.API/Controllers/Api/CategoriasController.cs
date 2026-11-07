using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ImportNet.Application.UseCases.Categorias.Commands;
using ImportNet.Application.UseCases.Categorias.Queries;
namespace ImportNet.API.Controllers.Api;
[ApiController]
[Route("api/categorias")]
[Authorize(Policy = "CatalogoLeer")]
public sealed class CategoriasController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct) => Ok(await mediator.Send(new GetAllCategoriasQuery(), ct));
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct) => Ok(await mediator.Send(new GetCategoriaByIdQuery(id), ct));
    [HttpPost, Authorize(Policy = "CatalogoEditar")]
    public async Task<IActionResult> Crear(CreateCategoriaCommand command, CancellationToken ct)
    {
        var id = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(Obtener), new { id }, new { id });
    }
    [HttpPut("{id:int}"), Authorize(Policy = "CatalogoEditar")]
    public async Task<IActionResult> Editar(int id, UpdateCategoriaCommand command, CancellationToken ct)
    {
        await mediator.Send(command with { Id = id }, ct);
        return NoContent();
    }
    [HttpDelete("{id:int}"), Authorize(Policy = "CatalogoEditar")]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        await mediator.Send(new DeleteCategoriaCommand(id), ct);
        return NoContent();
    }
}
