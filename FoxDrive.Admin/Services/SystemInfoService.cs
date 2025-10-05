using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;

namespace FoxDrive.Admin.Services;

public class SystemInfoService
{
    public float GetCpuUsage()
    {
        using var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        // First call always 0, need a small wait
        _ = cpuCounter.NextValue();
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
            return ((float)(total - avail) / total) * 100f;
        }
        return -1;
    }



    public Dictionary<string, float> GetDiskUsage(params string[] drives)
    {
        var result = new Dictionary<string, float>();
        foreach (var drive in drives)
        {
            var d = new DriveInfo(drive);
            if (d.IsReady)
            {
                float used = (float)(d.TotalSize - d.AvailableFreeSpace) / d.TotalSize * 100f;
                result[d.Name] = used;
            }
        }
        return result;
    }

    public bool IsProcessRunning(string name)
    {
        return Process.GetProcessesByName(name).Any();
    }

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
                sent += (bytesSent - last.sent) / 1024f / 1024f; // MB
                recv += (bytesRecv - last.recv) / 1024f / 1024f;
            }

            _lastSamples[key] = (bytesSent, bytesRecv);
        }

        return (sent, recv);
    }

    private Dictionary<string, (long sent, long recv)> _lastSamples = new();
}
