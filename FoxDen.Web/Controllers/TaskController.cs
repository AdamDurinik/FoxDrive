using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoxDen.Web.Controllers;

[Authorize]
[Route("tasks")]
public class TasksController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View(); // Views/Tasks/Index.cshtml
}
