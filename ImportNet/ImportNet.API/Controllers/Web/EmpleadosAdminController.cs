using ImportNet.API.Models;
using ImportNet.Application.UseCases.Empleados.Commands;
using ImportNet.Application.UseCases.Empleados.Queries;
using ImportNet.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImportNet.API.Controllers.Web;

[Authorize(Policy = "Administracion"), Route("empleados")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class EmpleadosAdminController(IMediator mediator) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? busqueda, bool? activo, CancellationToken ct)
        => View(new EmpleadoListado(await mediator.Send(new GetAllEmpleadosQuery(busqueda, activo), ct), busqueda, activo));
    [HttpGet("crear")]
    public IActionResult Crear() => View("Editar", new EmpleadoForm());
    [HttpGet("{id:int}/editar")]
    public async Task<IActionResult> Editar(int id, CancellationToken ct)
    {
        var e = await mediator.Send(new GetEmpleadoByIdQuery(id), ct);
        return View(new EmpleadoForm { Id = e.Id, UsuarioId = e.UsuarioId, Nombre = e.Nombre, Apellidos = e.Apellidos,
            DocumentoIdentidad = e.DocumentoIdentidad ?? "", Email = e.Email, Telefono = e.Telefono,
            Cargo = e.Cargo, Area = e.Area, Activo = e.Activo, Roles = e.Roles.ToArray() });
    }
    [HttpPost("guardar")]
    public async Task<IActionResult> Guardar(EmpleadoForm form, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            try
            {
                if (form.Id == 0) await mediator.Send(new CreateEmpleadoCommand(form.Datos(), form.Password ?? "", form.Activo, form.Roles), ct);
                else await mediator.Send(new UpdateEmpleadoCommand(form.Id, form.Datos(), form.Activo, form.Roles), ct);
                TempData["Mensaje"] = "Empleado y cuenta guardados. Los cambios de acceso se aplican a las sesiones en un máximo de un minuto.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex) when (ex is ReglaNegocioException or ConflictoException)
            { ModelState.AddModelError("", ex.Message); }
        }
        form.Password = null;
        ModelState.SetModelValue(nameof(form.Password), null, null);
        return View("Editar", form);
    }
    [HttpPost("{id:int}/estado")]
    public async Task<IActionResult> Estado(int id, bool activo, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new CambiarEstadoEmpleadoCommand(id, activo), ct);
            TempData["Mensaje"] = activo ? "Empleado activado." : "Empleado desactivado. Su historial se conserva.";
        }
        catch (ConflictoException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }
}
