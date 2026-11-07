using ImportNet.API.Models;
using ImportNet.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Antiforgery;
namespace ImportNet.API.Controllers.Web;

[Route("cuenta")]
public sealed class CuentaController(SignInManager<ApplicationUser> signIn, IAntiforgery antiforgery) : Controller
{
    [HttpGet("login"), AllowAnonymous]
    public IActionResult Login(string? returnUrl = null) => View(new LoginForm { ReturnUrl = returnUrl });
    [HttpPost("login"), AllowAnonymous]
    public async Task<IActionResult> Login(LoginForm form)
    {
        if (!ModelState.IsValid) return View(form);
        var result = await signIn.PasswordSignInAsync(form.Email, form.Password, false, lockoutOnFailure: true);
        if (!result.Succeeded) { ModelState.AddModelError("", "No se pudo iniciar sesión. Compruebe sus datos o espere si la cuenta está bloqueada."); return View(form); }
        return LocalRedirect(Url.IsLocalUrl(form.ReturnUrl) ? form.ReturnUrl! : "/");
    }
    [HttpPost("logout"), Authorize]
    public async Task<IActionResult> Logout() { await signIn.SignOutAsync(); return RedirectToAction(nameof(Login)); }
    [HttpGet("denegado")]
    public IActionResult Denegado() { Response.StatusCode = 403; return View(); }
    [HttpGet("csrf"), Authorize]
    public IActionResult Csrf()
    {
        Response.Headers.CacheControl = "no-store";
        return Json(new { token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });
    }
}
