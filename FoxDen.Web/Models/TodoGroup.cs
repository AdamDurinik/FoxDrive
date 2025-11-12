using System.ComponentModel.DataAnnotations;

namespace FoxDen.Web.Models
{
    public class TodoGroup  
    {
        public int Id { get; set; }

        [Required, MaxLength(120)]
        public string Name { get; set; } = string.Empty;

        public bool Done { get; set; }
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        public List<TodoItem> Items { get; set; } = new();
    }
}