namespace FoxDrive.Web.Models
{
    public class ShareGrant
    {
        public string From { get; set; } = "";
        public string To { get; set; } = "";
        public string Path { get; set; } = ""; // relative inside From's home
    }
}
