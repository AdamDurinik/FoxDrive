
namespace FoxDen.Web.Models.Recepie;
public class RecepieIngredient
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<RecepieIngredient> Substitutions { get; set; } = new();
}