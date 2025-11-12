using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoxDen.Web.Controllers;

[Authorize]
[Route("todo")]
public class TodoController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View(); // Views/Todo/Index.cshtml
}
