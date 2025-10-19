using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public class ShoppingController : Controller
{
    [HttpGet("/shopping")]
    public IActionResult Index() => View();
}
