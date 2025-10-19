using System.ComponentModel.DataAnnotations;

namespace FoxDen.Web.Models;

public class ShoppingItem
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = "";

    [MaxLength(50)]
    public string? Amount { get; set; }

    public DateTime? Date { get; set; } = DateTime.Now;

    [MaxLength(100)]
    public string? Shop { get; set; }

    public bool Bought { get; set; } = false;
}