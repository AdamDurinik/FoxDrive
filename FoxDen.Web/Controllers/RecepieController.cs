using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoxDen.Web.Models;
[Authorize]
[Route("recepies")]
public class RecepieController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();

    [HttpGet("create-recepie")]
    public IActionResult RecepieCreate() => View("RecepieCreate");

    [HttpGet("view-recepie/{id:int}")]
    public IActionResult RecepieView(int id) => View("RecepieView");

    [HttpGet("cook-recepie/{id:int}")]
    public IActionResult Cook(int id) => View("CookView");
}
