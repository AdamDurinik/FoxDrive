using FoxDrive.Web.Models;
using Microsoft.Extensions.Options;

namespace FoxDrive.Web.Services
{
    public class SharesService
    {
        private readonly List<ShareGrant> _grants;
        private readonly bool _sharedWriteEnabled;

        public SharesService(IConfiguration cfg)
        {
            _grants = cfg.GetSection("Sharing:Grants").Get<List<ShareGrant>>() ?? new();
            _sharedWriteEnabled = cfg.GetValue("Sharing:SharedWriteEnabled", false);
        }

        public IEnumerable<string> SendersFor(string toUser) =>
            _grants.Where(g => string.Equals(g.To, toUser, StringComparison.OrdinalIgnoreCase))
                   .Select(g => g.From)
                   .Distinct(StringComparer.OrdinalIgnoreCase);

        // Returns the grant that matches (if any) for access to "owner/relPath"
        private ShareGrant? FindGrant(string owner, string toUser, string relPath)
        {
            return _grants.Where(g =>
                    string.Equals(g.From, owner, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(g.To, toUser, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault(g =>
                {
                    var grantPrefix = (g.Path ?? "").Replace('\\','/').Trim('/');
                    var req = (relPath ?? "").Replace('\\','/').Trim('/');
                    return grantPrefix.Length == 0 || req.StartsWith(grantPrefix + "/", StringComparison.OrdinalIgnoreCase) || req.Equals(grantPrefix, StringComparison.OrdinalIgnoreCase);
                });
        }

        public bool CanRead(string currentUser, string owner, string relPath)
        {
            if (string.Equals(currentUser, owner, StringComparison.OrdinalIgnoreCase)) return true;
            return FindGrant(owner, currentUser, relPath) != null;
        }

        public bool CanWrite(string currentUser, string owner, string relPath)
        {
            if (string.Equals(currentUser, owner, StringComparison.OrdinalIgnoreCase)) return true;
            if (!_sharedWriteEnabled) return false;
            return FindGrant(owner, currentUser, relPath) != null;
        }
    }
}
