namespace FoxDrive.Web.Models
{
    public enum EntryKind { File, Folder }

    public class FileEntry
    {
        public string Name { get; set; } = "";
        public EntryKind Kind { get; set; }
        public long? Size { get; set; }
        public DateTime? LastModified { get; set; }
    }
}
