using ImportNet.Application.UseCases.Categorias.Commands;
using ImportNet.Application.UseCases.Categorias.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImportNet.API.Controllers.Api;

[ApiController]
[Route("api/categorias")]
[Authorize(Policy = "CatalogoLeer")]
public class CategoriasController : ControllerBase
{
    private readonly IMediator _mediator;

    public CategoriasController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct)
    {
        return Ok(await _mediator.Send(new GetAllCategoriasQuery(), ct));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct)
    {
        return Ok(await _mediator.Send(new GetCategoriaByIdQuery(id), ct));
    }

    [HttpPost]
    [Authorize(Policy = "CatalogoEditar")]
    public async Task<IActionResult> Crear(CreateCategoriaCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(Obtener), new { id }, new { id });
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "CatalogoEditar")]
    public async Task<IActionResult> Editar(int id, UpdateCategoriaCommand command, CancellationToken ct)
    {
        await _mediator.Send(command with { Id = id }, ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "CatalogoEditar")]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteCategoriaCommand(id), ct);
        return NoContent();
    }
}
