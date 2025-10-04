using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FoxHint.Admin.Services;

namespace FoxHint.Admin.Controllers;

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
        var cpu = _sys.GetCpuUsage();
        var mem = _sys.GetMemoryUsage();
        var disks = _sys.GetDiskUsage("C:\\", "D:\\", "F:\\");
        var net = _sys.GetNetworkUsage();

        var data = new
        {
            cpu = new { used = cpu.ToString("F1"), total = "100" },
            memory = new { used = mem.ToString("F1"), total = "100" },
            disks,
            network = new { sent = net.sentMbps.ToString("F2"), recv = net.recvMbps.ToString("F2") },
            apps = new {
                foxdrive = _sys.IsProcessRunning("FoxDrive"),
                portfolio = _sys.IsProcessRunning("Portfolio")
            }
        };

        return Ok(data);
    }

}
