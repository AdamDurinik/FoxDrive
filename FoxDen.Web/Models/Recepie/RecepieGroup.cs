
namespace FoxDen.Web.Models.Recepie;

public class RecepieGroup
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<RecepieVersion> Versions { get; set; } = new();

}