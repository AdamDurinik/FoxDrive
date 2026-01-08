
namespace FoxDen.Web.Models.Recepie;
public class RecepieItem
{
    public int Id { get; set; }
    public RecepieIngredient Ingredient { get; set; } = new();
    public float Quantity { get; set; }
    public RecepieQuantityType QuantityType { get; set; }

}