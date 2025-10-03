using FoxDrive.Web.Models;
using FoxDrive.Web.Options;
using Microsoft.Extensions.Options;

namespace FoxDrive.Web.Services
{
    public class FileStorageService
    {
        private readonly string _usersRoot; // ...\Data\Users
        private readonly string _sharedPath; // ...\Data\Shared

        public FileStorageService(IOptions<StorageOptions> options)
        {
            var dataRoot = options.Value.RootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var sharedRoot = options.Value.SharedPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            _usersRoot = Path.Combine(dataRoot, "Users");
            _sharedPath = sharedRoot;
            Directory.CreateDirectory(_usersRoot);
        }

        private string UserHome(string user) => Path.Combine(_usersRoot, user);

        private string MapPath(string ownerUser, string? relativePath)
        {
            relativePath ??= "";
            var home = UserHome(ownerUser);
            Directory.CreateDirectory(home);
            var combined = Path.GetFullPath(Path.Combine(home, relativePath));
            if (!combined.StartsWith(home, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Invalid path.");
            return combined;
        }

        public string MapToAbsolute(string owner, string relativePath)
        {
            var basePath = Path.GetFullPath(Path.Combine("D:\\FoxDrive\\Data\\users", owner ?? ""));
            var abs = Path.GetFullPath(Path.Combine(basePath, relativePath ?? ""));
            if (!abs.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                throw new IOException("Path traversal outside of owner root.");
            return abs;
        }


        public IEnumerable<FileEntry> List(string ownerUser, string? relativePath = "")
        {
            var path = MapPath(ownerUser, relativePath);
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            foreach (var d in Directory.EnumerateDirectories(path).OrderBy(Path.GetFileName))
            {
                yield return new FileEntry
                {
                    Name = Path.GetFileName(d),
                    Kind = EntryKind.Folder,
                    Size = null,
                    LastModified = Directory.GetLastWriteTimeUtc(d)
                };
            }
            foreach (var f in Directory.EnumerateFiles(path).OrderBy(Path.GetFileName))
            {
                var fi = new FileInfo(f);
                yield return new FileEntry
                {
                    Name = fi.Name,
                    Kind = EntryKind.File,
                    Size = fi.Length,
                    LastModified = fi.LastWriteTimeUtc
                };
            }
        }

        public void Save(string ownerUser, string relativePath, Stream fileStream)
        {
            var full = MapPath(ownerUser, relativePath);
            var dir = Path.GetDirectoryName(full)!;
            Directory.CreateDirectory(dir);
            using var fs = new FileStream(full, FileMode.Create, FileAccess.Write, FileShare.None);
            fileStream.CopyTo(fs);
        }

        public Stream Read(string ownerUser, string relativePath)
        {
            var full = MapPath(ownerUser, relativePath);
            return new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public void Delete(string ownerUser, string relativePath)
        {
            var full = MapPath(ownerUser, relativePath);
            if (File.Exists(full)) File.Delete(full);
            else if (Directory.Exists(full)) Directory.Delete(full, recursive: true);
        }

        public void Mkdir(string ownerUser, string? parentPath, string name)
        {
            var full = MapPath(ownerUser, Path.Combine(parentPath ?? "", name));
            Directory.CreateDirectory(full);
        }

        public void Rename(string ownerUser, string? parentPath, string from, string to)
        {
            var src = MapPath(ownerUser, Path.Combine(parentPath ?? "", from));
            var dst = MapPath(ownerUser, Path.Combine(parentPath ?? "", to));
            if (File.Exists(src)) File.Move(src, dst);
            else if (Directory.Exists(src)) Directory.Move(src, dst);
            else throw new FileNotFoundException("Source not found.");
        }

        public void Move(string ownerUserFrom, string fromPath, string name, string ownerUserTo, string toPath)
        {
            var src = MapPath(ownerUserFrom, Path.Combine(fromPath ?? "", name));
            var dstDir = MapPath(ownerUserTo, toPath ?? "");
            Directory.CreateDirectory(dstDir);
            var dst = Path.Combine(dstDir, Path.GetFileName(src));
            if (File.Exists(src)) File.Move(src, dst);
            else if (Directory.Exists(src)) Directory.Move(src, dst);
            else throw new FileNotFoundException("Source not found.");
        }
    }
}
