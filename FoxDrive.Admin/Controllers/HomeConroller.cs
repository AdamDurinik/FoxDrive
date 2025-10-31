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
            // safer: ignore drives that don't exist/inaccessible in prod
            Disks = _sys.GetDiskUsageSafe(@"C:\", @"F:\"),

            // robust in deploy regardless of exe vs. dll host
            FoxDriveRunning  = _sys.IsPortListening(5010),
            PortfolioRunning = _sys.IsPortListening(5173),
            FoxDenRunning    = _sys.IsPortListening(5228),
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
