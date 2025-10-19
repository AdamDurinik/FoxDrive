using FoxDrive.Admin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoxDrive.Admin.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly SystemInfoService _sys;
    public HomeController(SystemInfoService sys) => _sys = sys;

    public IActionResult Index() => RedirectToAction("Dashboard");

    public IActionResult Dashboard()
    {
        var model = new DashboardViewModel
        {
            Cpu = _sys.GetCpuUsage(),
            Memory = _sys.GetMemoryUsage(),
            Disks = _sys.GetDiskUsage("C:\\", "F:\\"),
            FoxDriveRunning = _sys.IsProcessRunning("FoxDrive.Web"),
            PortfolioRunning = _sys.IsProcessRunning("Portfolio.Web"),
            FoxDenRunning = _sys.IsProcessRunning("FoxDen.Web") 

        };
        return View(model);
    }
}

public class DashboardViewModel
{
    public float Cpu { get; set; }
    public float Memory { get; set; }
    public Dictionary<string, float> Disks { get; set; } = new();
    public bool FoxDriveRunning { get; set; }
    public bool PortfolioRunning { get; set; }
    public bool FoxDenRunning { get; set; }
}
