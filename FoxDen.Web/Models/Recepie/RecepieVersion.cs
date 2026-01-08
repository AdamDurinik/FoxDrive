using static System.Net.Mime.MediaTypeNames;


namespace FoxDen.Web.Models.Recepie;
public class RecepieVersion
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Servings { get; set; }

    public RecepieImage? Photo { get; set; } 

    public byte Rating { get; set; }

    public DateTime CreatedUtc { get; set; }

    public List<RecepieItem> Ingredients { get; set; } = new();
    public List<RecepieProcess> Steps { get; set; } = new();
}