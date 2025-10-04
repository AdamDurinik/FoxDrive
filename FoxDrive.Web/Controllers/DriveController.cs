using FoxDrive.Web.Models;
using FoxDrive.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using System.Security.Claims;
using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;


namespace FoxDrive.Web.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize]
    public class DriveController : ControllerBase
    {
        private readonly string _ffmpegPath;
        private readonly string _hlsCache;

        private readonly FileStorageService _storage;
        private readonly SharesService _shares;

        public DriveController(
            FileStorageService storage,
            SharesService shares,
            IConfiguration config,
            IWebHostEnvironment env)
        {
            _storage = storage;
            _shares  = shares;

            _ffmpegPath = config["Streaming:FfmpegPath"] ?? "ffmpeg"; // or full path to ffmpeg.exe
            _hlsCache   = config["Streaming:CacheDir"]
                        ?? Path.Combine(env.ContentRootPath, "Data", "streamcache");

            Directory.CreateDirectory(_hlsCache);
        }

        private static string HashKey(string s)
        {
            using var sha = SHA256.Create();
            var b = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(s));
            return Convert.ToHexString(b).ToLowerInvariant();
        }

        private string HlsDirForAbsolute(string absPath)
        {
            return Path.Combine(_hlsCache, HashKey(absPath));
        }

        private void EnsureHlsBuilt(string absInput, string outDir)
        {
            var manifest = Path.Combine(outDir, "index.m3u8");
            if (System.IO.File.Exists(manifest)) return;

            var test = System.IO.File.Exists(manifest);
            Directory.CreateDirectory(outDir);

            var psi = new ProcessStartInfo {
                FileName = _ffmpegPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = false,              // ❗ DO NOT REDIRECT (prevents blocking)
                WorkingDirectory = outDir                   // logs and temp files land here
            };

            // Build args (VOD HLS)
            psi.ArgumentList.Add("-hide_banner");
            psi.ArgumentList.Add("-nostdin");
            psi.ArgumentList.Add("-y");
            psi.ArgumentList.Add("-loglevel"); psi.ArgumentList.Add("error");
            psi.ArgumentList.Add("-report");

            // --- GPU acceleration ---
            psi.ArgumentList.Add("-hwaccel"); psi.ArgumentList.Add("cuda");
            psi.ArgumentList.Add("-hwaccel_output_format"); psi.ArgumentList.Add("cuda");

            // --- Input ---
            psi.ArgumentList.Add("-i"); psi.ArgumentList.Add(absInput);

            // --- Video ---
            psi.ArgumentList.Add("-map"); psi.ArgumentList.Add("0:v:0?");
            psi.ArgumentList.Add("-vf"); psi.ArgumentList.Add("scale_cuda=w=1920:h=-2,hwdownload,format=nv12");
            psi.ArgumentList.Add("-c:v"); psi.ArgumentList.Add("h264_nvenc");
            psi.ArgumentList.Add("-preset"); psi.ArgumentList.Add("p5");         // Balanced speed/quality
            psi.ArgumentList.Add("-b:v"); psi.ArgumentList.Add("5M");
            psi.ArgumentList.Add("-maxrate"); psi.ArgumentList.Add("6M");
            psi.ArgumentList.Add("-bufsize"); psi.ArgumentList.Add("10M");
            psi.ArgumentList.Add("-rc:v"); psi.ArgumentList.Add("vbr");          // Variable bitrate control

            // --- Audio ---
            psi.ArgumentList.Add("-map"); psi.ArgumentList.Add("0:a:0?");
            psi.ArgumentList.Add("-c:a"); psi.ArgumentList.Add("aac");
            psi.ArgumentList.Add("-b:a"); psi.ArgumentList.Add("128k");
            psi.ArgumentList.Add("-ar"); psi.ArgumentList.Add("48000");
            psi.ArgumentList.Add("-ac"); psi.ArgumentList.Add("2");

            // --- Output (HLS) ---
            psi.ArgumentList.Add("-f"); psi.ArgumentList.Add("hls");
            psi.ArgumentList.Add("-hls_time"); psi.ArgumentList.Add("4");
            psi.ArgumentList.Add("-hls_list_size"); psi.ArgumentList.Add("0");
            psi.ArgumentList.Add("-hls_flags"); psi.ArgumentList.Add("append_list+omit_endlist+independent_segments");
            psi.ArgumentList.Add("-hls_playlist_type"); psi.ArgumentList.Add("event");
            psi.ArgumentList.Add("-hls_segment_filename");
            psi.ArgumentList.Add(Path.Combine(outDir, "seg%05d.ts"));
            psi.ArgumentList.Add(Path.Combine(outDir, "index.m3u8"));


            // Start ffmpeg in background (do NOT await; it will build in the cache)
            Process.Start(psi);
        }

        private string CurrentUser => User.Identity?.Name ?? "unknown";

        // Virtual path rules:
        //   "" or "foo/bar"                  -> owner = currentUser, rel = path
        //   "@shared"                        -> list senders who shared with me (virtual folders)
        //   "@shared/{fromUser}"             -> list the root of grant Path for that sender
        //   "@shared/{fromUser}/sub/dir"     -> list within that sender’s share
        // TODO: To be changed to db calls, not json
        private (string owner, string rel, bool isShared) Resolve(string? path)
        {
            path ??= "";
            path = path.Replace('\\', '/').Trim('/');
            if (!path.StartsWith("@shared", StringComparison.OrdinalIgnoreCase))
            {
                return (CurrentUser, path, false);
            }

            var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) // "@shared" root
                return ("__virtual__", "", true);

            var fromUser = parts[1];
            var rel = string.Join('/', parts.Skip(2));
            return (fromUser, rel, true);
        }

        private static string GetContentType(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext switch
            {
                ".mp4" => "video/mp4",
                ".m4v" => "video/mp4",
                ".webm" => "video/webm",
                ".ogv" => "video/ogg",
                ".mov" => "video/quicktime",
                ".mkv" => "video/x-matroska",

                ".mp3" => "audio/mpeg",
                ".m4a" => "audio/mp4",
                ".aac" => "audio/aac",
                ".wav" => "audio/wav",
                ".ogg" => "audio/ogg",
                _ => new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider()
                        .TryGetContentType(fileName, out var ct) ? ct : "application/octet-stream"
            };
        }
        [HttpGet("hls/manifest")]
        public IActionResult HlsManifest([FromQuery] string? path, [FromQuery] string name)
        {
            var (owner, rel, _) = Resolve(path);
            var fullRel = Path.Combine(rel ?? "", name);
            if (!_shares.CanRead(CurrentUser, owner, fullRel)) return Forbid();

            var abs = _storage.MapToAbsolute(owner, fullRel);
            if (!System.IO.File.Exists(abs)) return NotFound();

            var dir = HlsDirForAbsolute(abs);
            EnsureHlsBuilt(abs, dir);

            var manifestPath = Path.Combine(dir, "index.m3u8");

            // 🔒 make sure m3u8 isn’t cached by browser/CDN
            Response.Headers["Cache-Control"]     = "no-store, no-cache, must-revalidate";
            Response.Headers["Pragma"]            = "no-cache";
            Response.Headers["Expires"]           = "0";
            Response.Headers["Surrogate-Control"] = "no-store";

            // ⏳ wait up to ~2s until the playlist is actually playable (has at least one .ts)
            var sw = System.Diagnostics.Stopwatch.StartNew();
            string text = string.Empty;

            while (sw.Elapsed < TimeSpan.FromSeconds(2))
            {
                if (System.IO.File.Exists(manifestPath))
                {
                    using var fs = new FileStream(manifestPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var sr = new StreamReader(fs);
                    text = sr.ReadToEnd();
                    if (text.IndexOf(".ts", StringComparison.OrdinalIgnoreCase) >= 0)
                        break;
                }
                else
                {
                    // fallback: if first seg exists, serve a tiny one-segment playlist
                    if (Directory.Exists(dir))
                    {
                        var firstSeg = Directory.GetFiles(dir, "seg*.ts").OrderBy(f => f).FirstOrDefault();
                        if (firstSeg != null)
                        {
                            text = "#EXTM3U\n#EXT-X-VERSION:3\n#EXT-X-TARGETDURATION:4\n#EXT-X-MEDIA-SEQUENCE:0\n#EXTINF:4.0,\n" +
                                Path.GetFileName(firstSeg) + "\n";
                            break;
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(text))
                return StatusCode(202, "Preparing stream...");

            // rewrite segment URIs to your API
            var lines = text.Replace("\r", "").Split('\n', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                var l = lines[i].Trim();
                if (!l.StartsWith("#") && l.EndsWith(".ts", StringComparison.OrdinalIgnoreCase))
                {
                    lines[i] =
                        $"/api/hls/segment?path={Uri.EscapeDataString(path ?? "")}&name={Uri.EscapeDataString(name)}&file={Uri.EscapeDataString(l)}";
                }
            }

            var outText = string.Join("\n", lines) + "\n";
            return Content(outText, "application/vnd.apple.mpegurl");
        }

        [HttpGet("hls/segment")]
        public IActionResult HlsSegment([FromQuery] string? path, [FromQuery] string name, [FromQuery] string file)
        {
            var (owner, rel, _) = Resolve(path);
            var fullRel = Path.Combine(rel ?? "", name);
            if (!_shares.CanRead(CurrentUser, owner, fullRel)) return Forbid();

            if (string.IsNullOrEmpty(file) || file.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || file.Contains(".."))
                return BadRequest("Bad segment name.");

            var abs = _storage.MapToAbsolute(owner, fullRel);
            var dir = HlsDirForAbsolute(abs);
            var segAbs = Path.Combine(dir, file);
            if (!System.IO.File.Exists(segAbs)) return NotFound();

            // TS mime is fine; ranges not needed (they are tiny)
            Response.Headers["Cache-Control"] = "no-store";
            return PhysicalFile(segAbs, "video/mp2t");
        }


        [HttpGet("list")]
        public ActionResult<IEnumerable<FileEntry>> List([FromQuery] string? path = "")
        {
            var (owner, rel, isShared) = Resolve(path);

            // Virtual listing: "@shared" root -> show senders as folders
            if (isShared && owner == "__virtual__")
            {
                var senders = _shares.SendersFor(CurrentUser);
                var pseudo = senders
                    .OrderBy(s => s)
                    .Select(s => new FileEntry { Name = s, Kind = EntryKind.Folder });
                return Ok(pseudo);
            }

            if (!_shares.CanRead(CurrentUser, owner, rel))
                return Forbid();

            var list = _storage.List(owner, rel);
            return Ok(list);
        }

        [HttpPost("mkdir")]
        public IActionResult Mkdir([FromQuery] string? path, [FromQuery] string name)
        {
            var (owner, rel, _) = Resolve(path);
            if (!_shares.CanWrite(CurrentUser, owner, rel)) return Forbid();
            _storage.Mkdir(owner, rel, name);
            return NoContent();
        }

        [HttpPost("upload")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> Upload([FromQuery] string? path, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var (owner, rel, _) = Resolve(path);
            if (!_shares.CanWrite(CurrentUser, owner, rel ?? "")) return Forbid();

            var targetRel = Path.Combine(rel ?? "", file.FileName);
            var targetAbs = _storage.MapToAbsolute(owner, targetRel);
            Directory.CreateDirectory(Path.GetDirectoryName(targetAbs)!);

            await using (var fs = System.IO.File.Create(targetAbs))
            {
                await file.CopyToAsync(fs);
            }

            return NoContent();
        }

        [HttpPost("upload/chunk")]
        [DisableRequestSizeLimit] // each chunk is small anyway (e.g., 8–16 MB)
        public async Task<IActionResult> UploadChunk(
            [FromQuery] string? path,
            [FromQuery] string fileName,
            [FromQuery] string uploadId,
            [FromQuery] int index,
            [FromQuery] int total,
            IFormFile chunk)
        {
            if (string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(uploadId))
                return BadRequest("Missing fileName/uploadId.");
            if (index < 0 || total <= 0 || index >= total)
                return BadRequest("Bad index/total.");
            if (chunk == null || chunk.Length == 0)
                return BadRequest("Empty chunk.");

            var (owner, rel, _) = Resolve(path);
            if (!_shares.CanWrite(CurrentUser, owner, rel ?? "")) return Forbid();

            // Save to a temp folder under the target dir: <target>/.uploads/<uploadId>/
            var tmpRel = Path.Combine(rel ?? "", ".uploads", uploadId);
            var tmpAbs = _storage.MapToAbsolute(owner, tmpRel);
            Directory.CreateDirectory(tmpAbs);

            var partName = $"{index:D6}.part";
            var partAbs = Path.Combine(tmpAbs, partName);
            // Write this chunk
            await using (var fs = System.IO.File.Create(partAbs))
            {
                await chunk.CopyToAsync(fs);
            }

            // If not last chunk, return now
            if (index != total - 1) return NoContent();

            // Last chunk received -> assemble
            var targetRel = Path.Combine(rel ?? "", fileName);
            var targetAbs = _storage.MapToAbsolute(owner, targetRel);
            Directory.CreateDirectory(Path.GetDirectoryName(targetAbs)!);

            await using (var output = new FileStream(targetAbs, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 128, useAsync: true))
            {
                for (int i = 0; i < total; i++)
                {
                    var partPath = Path.Combine(tmpAbs, $"{i:D6}.part");
                    if (!System.IO.File.Exists(partPath))
                        return StatusCode(StatusCodes.Status500InternalServerError, $"Missing chunk {i}.");
                    await using var input = new FileStream(partPath, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 128, useAsync: true);
                    await input.CopyToAsync(output);
                }
            }

            // Cleanup temp
            try
            {
                Directory.Delete(tmpAbs, recursive: true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: failed to delete temp upload dir {tmpAbs}: {ex}");
            }

            return NoContent();
        }


        // [HttpGet("download")]
        // public IActionResult Download([FromQuery] string? path, [FromQuery] string name)
        // {
        //     var (owner, rel, _) = Resolve(path);
        //     var fullRel = Path.Combine(rel, name);
        //     if (!_shares.CanRead(CurrentUser, owner, fullRel)) return Forbid();

        //     var stream = _storage.Read(owner, fullRel);
        //     return File(stream, "application/octet-stream", name);
        // }

        // [HttpGet("open")]
        // public IActionResult Open([FromQuery] string? path, [FromQuery] string name)
        // {
        //     var (owner, rel, _) = Resolve(path);
        //     var fullRel = Path.Combine(rel, name);
        //     if (!_shares.CanRead(CurrentUser, owner, fullRel)) return Forbid();

        //     var ext = Path.GetExtension(name).ToLowerInvariant();
        //     var mime = ext switch
        //     {
        //         ".png" => "image/png",
        //         ".jpg" or ".jpeg" => "image/jpeg",
        //         ".gif" => "image/gif",
        //         ".webp" => "image/webp",
        //         ".svg" => "image/svg+xml",
        //         ".bmp" => "image/bmp",
        //         ".pdf" => "application/pdf",
        //         _ => "application/octet-stream"
        //     };

        //     var stream = _storage.Read(owner, fullRel);
        //     return File(stream, mime);
        // }
        [HttpGet("download")]
        public IActionResult Download([FromQuery] string? path, [FromQuery] string name)
        {
            var (owner, rel, _) = Resolve(path);
            var fullRel = Path.Combine(rel ?? "", name);
            if (!_shares.CanRead(CurrentUser, owner, fullRel)) return Forbid();

            var abs = _storage.MapToAbsolute(owner, fullRel);
            if (!System.IO.File.Exists(abs)) return NotFound();

            // ✅ Range support (resume/seek) enabled here
            return PhysicalFile(abs, GetContentType(name), fileDownloadName: name, enableRangeProcessing: true);
        }

        [HttpGet("open")]
        public IActionResult Open([FromQuery] string? path, [FromQuery] string name)
        {
            var (owner, rel, _) = Resolve(path);
            var fullRel = Path.Combine(rel ?? "", name);
            if (!_shares.CanRead(CurrentUser, owner, fullRel)) return Forbid();

            // Map to absolute on disk
            var abs = _storage.MapToAbsolute(owner, fullRel);
            if (!System.IO.File.Exists(abs)) return NotFound();

            // Detect content type (covers video/mp4, video/webm, video/ogg, images, pdf, etc.)
            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(name, out var contentType))
                contentType = "application/octet-stream";

            // ✅ Range enabled for seeking in the <video> element
            return PhysicalFile(abs, contentType, enableRangeProcessing: true);
        }

        [HttpDelete("delete")]
        public IActionResult Delete([FromQuery] string? path, [FromQuery] string name)
        {
            var (owner, rel, _) = Resolve(path);
            if (!_shares.CanWrite(CurrentUser, owner, rel)) return Forbid();
            _storage.Delete(owner, Path.Combine(rel, name));
            return NoContent();
        }

        [HttpPost("rename")]
        public IActionResult Rename([FromQuery] string? path, [FromQuery] string from, [FromQuery] string to)
        {
            var (owner, rel, _) = Resolve(path);
            if (!_shares.CanWrite(CurrentUser, owner, rel)) return Forbid();
            _storage.Rename(owner, rel, from, to);
            return NoContent();
        }

        public record MoveRequest(string? FromPath, string Name, string? ToPath);
        [HttpPost("move")]
        public IActionResult Move([FromBody] MoveRequest req)
        {
            var (fromOwner, fromRel, _) = Resolve(req.FromPath ?? "");
            var (toOwner, toRel, _) = Resolve(req.ToPath ?? "");

            if (!_shares.CanWrite(CurrentUser, fromOwner, fromRel)) return Forbid();
            if (!_shares.CanWrite(CurrentUser, toOwner, toRel)) return Forbid();

            _storage.Move(fromOwner, fromRel, req.Name, toOwner, toRel);
            return NoContent();
        }
    }
}
