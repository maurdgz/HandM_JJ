using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace ImportNet.API.Controllers.Web;
[Authorize]
public sealed class InicioController : Controller
{
    public IActionResult Index() => View();
}
