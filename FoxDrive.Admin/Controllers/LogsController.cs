using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.RegularExpressions;
using FoxDrive.Admin.Services;

[ApiController]
[Route("api/logs")]
public class LogsController : ControllerBase
{
    private const string McLogPath = @"C:\Users\Workshop\Desktop\Minecraft\The ships dont lie\logs\latest.log"; 
    private const string SupLogDir = @"C:\Users\Workshop\Desktop\ServerStart\logs";

    private readonly RconService _rcon;
    public LogsController(RconService rcon) { _rcon = rcon; }

    [HttpGet("minecraft")]
    public IActionResult Minecraft([FromQuery] int tail = 1000)
    {
        return Content(ReadTail(McLogPath, tail), "text/plain; charset=utf-8");
    }

    public record RconReq(string Command);

    [HttpPost("exec")]
    public async Task<IActionResult> Exec([FromBody] RconReq req)
    {
        if (req is null || string.IsNullOrWhiteSpace(req.Command))
            return BadRequest("Missing command");

        var output = await _rcon.ExecAsync(req.Command);
        return Ok(new { ok = true, output });
    }

    [HttpGet("supervisor")]
    public IActionResult Supervisor([FromQuery] int tail = 500)
    {
        var sup = GetNewestSupervisor();
        if (sup == null) return Content("[no supervisor logs]", "text/plain");
        return Content(ReadTail(sup.FullName, tail), "text/plain; charset=utf-8");
    }


    [HttpGet("last-crash")]
    public IActionResult LastCrash()
    {
        var sup = GetNewestSupervisor();
        if (sup == null) return Ok(new { when = (string?)null, exitCode = (int?)null, fileName = (string?)null, excerpt = (string?)null });
        var lines = ReadAllLinesShared(sup.FullName);
        if (lines.Length == 0) return Ok(new { when = (string?)null, exitCode = (int?)null, fileName = sup.Name, excerpt = (string?)null });


        var rxExit = new Regex(@"^\[(?<ts>[^\]]+)\]\s*minecraft exited code=(?<code>\d+)", RegexOptions.IgnoreCase);
        for (int i = lines.Length - 1; i >= 0; i--)
        {
            var m = rxExit.Match(lines[i]);
            if (!m.Success) continue;
            if (!int.TryParse(m.Groups["code"].Value, out var code) || code == 0) continue;


            // Find the mc tail block after this exit (logged by supervisor)
            var excerpt = new StringBuilder();
            bool inTail = false;
            for (int j = i + 1; j < lines.Length; j++)
            {
                var s = lines[j];
                if (s.Contains("[mc tail start]")) { inTail = true; continue; }
                if (s.Contains("[mc tail end]")) break;
                if (inTail) excerpt.AppendLine(s);
            }


            // If nothing captured, fall back to lines around the exit
            if (excerpt.Length == 0)
            {
                for (int j = Math.Max(0, i - 40); j < Math.Min(lines.Length, i + 40); j++)
                    excerpt.AppendLine(lines[j]);
            }


            // Highlight: keep only ERROR/FATAL/Exception lines if present
            var exLines = excerpt.ToString().Split('\n');
            var picks = exLines.Where(l => Regex.IsMatch(l, @"(FATAL|ERROR|Exception|Caused by|^\tat )", RegexOptions.IgnoreCase)).ToList();
            var resultText = picks.Count > 0 ? string.Join("\n", picks.TakeLast(80)) : string.Join("\n", exLines.TakeLast(80));


            return Ok(new
            {
                when = m.Groups["ts"].Value,
                exitCode = code,
                fileName = sup.Name,
                excerpt = resultText
            });
        }


        return Ok(new { when = (string?)null, exitCode = (int?)null, fileName = sup.Name, excerpt = (string?)null });
    }


    private static FileInfo? GetNewestSupervisor()
    {
        var dir = new DirectoryInfo(SupLogDir);
        if (!dir.Exists) return null;
        return dir.GetFiles("supervisor-*.log").OrderByDescending(f => f.LastWriteTimeUtc).FirstOrDefault();
    }


    private static string ReadTail(string path, int maxLines)
    {
        try
        {
            if (!System.IO.File.Exists(path)) return path + " - [file not found]";
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var sr = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var q = new Queue<string>(capacity: Math.Max(16, maxLines));
            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine();
                if (line is null) break;
                if (q.Count >= maxLines) q.Dequeue();
                q.Enqueue(line);
            }
            return string.Join("\n", q);
        }
        catch (Exception ex) { return "[read error] " + ex.Message; }
    }


    private static string[] ReadAllLinesShared(string path)
    {
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var sr = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var list = new List<string>(2048);
            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine();
                if (line is null) break;
                list.Add(line);
            }
            return list.ToArray();
        }
        catch { return Array.Empty<string>(); }
    }
}