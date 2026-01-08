using static System.Net.Mime.MediaTypeNames;


namespace FoxDen.Web.Models.Recepie;
public class RecepieProcess
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public RecepieImage? Photo { get; set; }
    public List<RecepieItem> Ingredients { get; set; } = new();
}