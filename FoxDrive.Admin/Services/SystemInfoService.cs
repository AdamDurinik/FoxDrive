using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.IO;

namespace FoxDrive.Admin.Services;

public class SystemInfoService
{
    public float GetCpuUsage()
    {
        using var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        _ = cpuCounter.NextValue();           // warm-up
        Thread.Sleep(500);
        return cpuCounter.NextValue();
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
        public MEMORYSTATUSEX() => dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

    public float GetMemoryUsage()
    {
        var memStatus = new MEMORYSTATUSEX();
        if (GlobalMemoryStatusEx(memStatus))
        {
            ulong total = memStatus.ullTotalPhys;
            ulong avail = memStatus.ullAvailPhys;
            return (float)(total - avail) / total * 100f;
        }
        return -1;
    }

    // Original method (kept)
    public Dictionary<string, float> GetDiskUsage(params string[] drives)
    {
        var result = new Dictionary<string, float>();
        foreach (var drive in drives)
        {
            var d = new DriveInfo(drive);
            if (d.IsReady)
            {
                float used = (float)(d.TotalSize - d.AvailableFreeSpace) / d.TotalSize * 100f;
                result[d.Name.TrimEnd('\\')] = used;
            }
        }
        return result;
    }

    // Safer variant for prod: skips missing/inaccessible drives automatically
    public Dictionary<string, float> GetDiskUsageSafe(params string[] roots)
    {
        if (roots == null || roots.Length == 0)
        {
            roots = DriveInfo.GetDrives()
                .Where(d => d.IsReady && d.DriveType == DriveType.Fixed)
                .Select(d => d.RootDirectory.FullName)
                .ToArray();
        }

        var result = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in roots.Where(p => !string.IsNullOrWhiteSpace(p)))
        {
            try
            {
                var di = new DriveInfo(Path.GetPathRoot(r)!);
                if (!di.IsReady) continue;

                var used = di.TotalSize - di.AvailableFreeSpace;
                var pct = di.TotalSize > 0 ? (float)(used * 100.0 / di.TotalSize) : 0f;
                result[di.Name.TrimEnd('\\')] = pct;
            }
            catch
            {
                // ignore non-existent/inaccessible drives
            }
        }
        return result;
    }

    // Original method (kept) – process name only (works for self-contained exe)
    public bool IsProcessRunning(string name)
    {
        return Process.GetProcessesByName(name).Any();
    }

    /// <summary>
    /// Robust in deployment: true if something is listening on localhost:port.
    /// Works whether the app runs as .exe or as dotnet + .dll.
    /// </summary>
    public bool IsPortListening(int port, int timeoutMs = 500)
    {
        try
        {
            using var c = new TcpClient();
            var ar = c.BeginConnect("127.0.0.1", port, null, null);
            if (!ar.AsyncWaitHandle.WaitOne(timeoutMs)) return false;
            return c.Connected;
        }
        catch
        {
            return false;
        }
    }

    // Your network sampler (unchanged; note it returns MB delta since last call, not Mbps)
    public (float sentMbps, float recvMbps) GetNetworkUsage(string? nicName = null)
    {
        var nics = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up &&
                        n.NetworkInterfaceType != NetworkInterfaceType.Loopback);

        if (!string.IsNullOrEmpty(nicName))
            nics = nics.Where(n => n.Name == nicName);

        float sent = 0, recv = 0;

        foreach (var nic in nics)
        {
            var stats = nic.GetIPv4Statistics();
            var key = nic.Id;

            long bytesSent = stats.BytesSent;
            long bytesRecv = stats.BytesReceived;

            if (_lastSamples.TryGetValue(key, out var last))
            {
                sent += (bytesSent - last.sent) / 1024f / 1024f; // MB delta since last call
                recv += (bytesRecv - last.recv) / 1024f / 1024f;
            }

            _lastSamples[key] = (bytesSent, bytesRecv);
        }

        return (sent, recv);
    }

    private readonly Dictionary<string, (long sent, long recv)> _lastSamples = new();
}
