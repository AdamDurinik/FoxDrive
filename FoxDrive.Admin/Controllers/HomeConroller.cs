using FoxHint.Admin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoxHint.Admin.Controllers;

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
            PortfolioRunning = _sys.IsProcessRunning("FoxHint.Ui") // adjust if different
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
}
