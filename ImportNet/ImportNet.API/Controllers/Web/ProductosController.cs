using MediatR;
using ImportNet.API.Models;
using ImportNet.Domain.Entities;
using ImportNet.Application.UseCases.Productos.Commands;
using ImportNet.Application.UseCases.Productos.Queries;
using ImportNet.Application.UseCases.Categorias.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace ImportNet.API.Controllers.Web;
[Authorize(Policy = "CatalogoLeer"), Route("productos")]
public sealed class ProductosController(IMediator mediator) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct) => View(await mediator.Send(new GetAllProductosQuery(), ct));
    [HttpGet("crear"), HttpGet("{id:int}/editar"), Authorize(Policy = "CatalogoEditar")]
    public async Task<IActionResult> Editar(int? id, CancellationToken ct)
    {
        var form = new ProductoForm();
        if (id.HasValue)
        {
            var item = await mediator.Send(new GetProductoByIdQuery(id.Value), ct);
            form = new ProductoForm { Id = item.Id, Codigo = item.Codigo, Nombre = item.Nombre, Descripcion = item.Descripcion, UnidadMedida = item.UnidadMedida, Peso = item.Peso, PrecioVenta = item.PrecioVenta, PartidaArancelaria = item.PartidaArancelaria, CategoriaId = item.CategoriaId, StockMinimo = item.StockMinimo };
        }
        form.Categorias = await mediator.Send(new GetAllCategoriasQuery(), ct);
        return View(form);
    }
    [HttpPost("guardar"), Authorize(Policy = "CatalogoEditar")]
    public async Task<IActionResult> Guardar(ProductoForm form, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            try
            {
                if (form.Id == 0) await mediator.Send(new CreateProductoCommand(form.Codigo, form.Nombre, form.Descripcion, form.UnidadMedida, form.Peso, form.PrecioVenta, form.PartidaArancelaria, form.CategoriaId, form.StockMinimo), ct);
                else await mediator.Send(new UpdateProductoCommand(form.Id, form.Codigo, form.Nombre, form.Descripcion, form.UnidadMedida, form.Peso, form.PrecioVenta, form.PartidaArancelaria, form.CategoriaId, form.StockMinimo), ct);
                TempData["Mensaje"] = "Registro guardado.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex) when (ex is ReglaNegocioException or ConflictoException)
            { ModelState.AddModelError("", ex.Message); }
        }
        form.Categorias = await mediator.Send(new GetAllCategoriasQuery(), ct);
        return View("Editar", form);
    }
    [HttpPost("{id:int}/eliminar"), Authorize(Policy = "CatalogoEditar")]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        try { await mediator.Send(new DeleteProductoCommand(id), ct); TempData["Mensaje"] = "Registro eliminado."; }
        catch (ConflictoException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }
}
