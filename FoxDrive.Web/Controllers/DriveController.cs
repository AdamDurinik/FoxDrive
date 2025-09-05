using FoxDrive.Web.Models;
using FoxDrive.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using System.Security.Claims;

namespace FoxDrive.Web.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize]
    public class DriveController : ControllerBase
    {
        private readonly FileStorageService _storage;
        private readonly SharesService _shares;

        public DriveController(FileStorageService storage, SharesService shares)
        {
            _storage = storage;
            _shares = shares;
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
            var provider = new FileExtensionContentTypeProvider();
            return provider.TryGetContentType(fileName, out var ct) ? ct : "application/octet-stream";
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
            var tmpRel   = Path.Combine(rel ?? "", ".uploads", uploadId);
            var tmpAbs   = _storage.MapToAbsolute(owner, tmpRel);
            Directory.CreateDirectory(tmpAbs);

            var partName = $"{index:D6}.part";
            var partAbs  = Path.Combine(tmpAbs, partName);

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
            try { Directory.Delete(tmpAbs, recursive: true); } catch { /* TODO:logging*/ }

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

            var abs = _storage.MapToAbsolute(owner, fullRel);
            if (!System.IO.File.Exists(abs)) return NotFound();

            // also enable range for inline previews (images/video/pdf seeking)
            return PhysicalFile(abs, GetContentType(name),fileDownloadName: name, enableRangeProcessing: true);
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
            var (toOwner, toRel, _)     = Resolve(req.ToPath   ?? "");

            if (!_shares.CanWrite(CurrentUser, fromOwner, fromRel)) return Forbid();
            if (!_shares.CanWrite(CurrentUser, toOwner, toRel))     return Forbid();

            _storage.Move(fromOwner, fromRel, req.Name, toOwner, toRel);
            return NoContent();
        }
    }
}
