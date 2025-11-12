using System.Net;
using System.Net.Sockets;
using CoreRCON;

namespace FoxDrive.Admin.Services;

public class RconService
{
    private readonly IPAddress _ip;
    private readonly ushort _port;
    private readonly string _password;

    public RconService(IConfiguration cfg)
    {
        var host = cfg["Rcon:Host"] ?? "127.0.0.1";
        if (!IPAddress.TryParse(host, out _ip))
        {
            // Resolve hostname to IPv4
            _ip = Dns.GetHostEntry(host).AddressList
                     .First(a => a.AddressFamily == AddressFamily.InterNetwork);
        }

        _port = ushort.TryParse(cfg["Rcon:Port"], out var p) ? p : (ushort)25575;
        _password = cfg["Rcon:Password"] ?? Environment.GetEnvironmentVariable("RCON_PASSWORD") ?? "GodAi";
    }

    public async Task<string> ExecAsync(string command, int timeoutMs = 5000)
    {
        using var rcon = new RCON(_ip, _port, _password);
        await rcon.ConnectAsync(); // no CancellationToken overload

        var sendTask = rcon.SendCommandAsync(command); // no CancellationToken overload
        var completed = await Task.WhenAny(sendTask, Task.Delay(timeoutMs));
        if (completed != sendTask) return "[timeout]";
        return await sendTask ?? string.Empty;
    }
}
