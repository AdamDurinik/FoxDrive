using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FoxDrive.Admin.Services;

namespace FoxDrive.Admin.Controllers;

[ApiController]
[Route("api/system")]
[Authorize]
public class SystemController : ControllerBase
{
    private readonly SystemInfoService _sys;

    public SystemController(SystemInfoService sys)
    {
        _sys = sys;
    }

    [HttpGet("stats")]
    public IActionResult Stats()
    {
        var cpu   = _sys.GetCpuUsage();
        var mem   = _sys.GetMemoryUsage();
        var disks = _sys.GetDiskUsageSafe(@"C:\", @"D:\", @"F:\");   // skips missing drives
        var net   = _sys.GetNetworkUsage();

        var data = new
        {
            cpu    = new { used = cpu.ToString("F1"), total = "100" },
            memory = new { used = mem.ToString("F1"), total = "100" },
            disks,
            network = new { sent = net.sentMbps.ToString("F2"), recv = net.recvMbps.ToString("F2") },
            apps = new
            {
                foxdrive  = _sys.IsPortListening(5010),
                portfolio = _sys.IsPortListening(5173),
                foxden    = _sys.IsPortListening(5228),
            }
        };

        return Ok(data);
    }
}
