using MediatR;
using ImportNet.API.Models;
using ImportNet.Domain.Entities;
using ImportNet.Application.UseCases.Categorias.Commands;
using ImportNet.Application.UseCases.Categorias.Queries;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace ImportNet.API.Controllers.Web;
[Authorize(Policy = "CatalogoLeer"), Route("categorias")]
public sealed class CategoriasController(IMediator mediator) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct) => View(await mediator.Send(new GetAllCategoriasQuery(), ct));
    [HttpGet("crear"), HttpGet("{id:int}/editar"), Authorize(Policy = "CatalogoEditar")]
    public async Task<IActionResult> Editar(int? id, CancellationToken ct)
    {
        var form = new CategoriaForm();
        if (id.HasValue)
        {
            var item = await mediator.Send(new GetCategoriaByIdQuery(id.Value), ct);
            form = new CategoriaForm { Id = item.Id, Nombre = item.Nombre };
        }

        return View(form);
    }
    [HttpPost("guardar"), Authorize(Policy = "CatalogoEditar")]
    public async Task<IActionResult> Guardar(CategoriaForm form, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            try
            {
                if (form.Id == 0) await mediator.Send(new CreateCategoriaCommand(form.Nombre), ct);
                else await mediator.Send(new UpdateCategoriaCommand(form.Id, form.Nombre), ct);
                TempData["Mensaje"] = "Registro guardado.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex) when (ex is ReglaNegocioException or ConflictoException)
            { ModelState.AddModelError("", ex.Message); }
        }

        return View("Editar", form);
    }
    [HttpPost("{id:int}/eliminar"), Authorize(Policy = "CatalogoEditar")]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        try { await mediator.Send(new DeleteCategoriaCommand(id), ct); TempData["Mensaje"] = "Registro eliminado."; }
        catch (ConflictoException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }
}
