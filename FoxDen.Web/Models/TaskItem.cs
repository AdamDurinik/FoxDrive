using System.ComponentModel.DataAnnotations;

namespace FoxDen.Web.Models
{
    public class TaskItem
    {
        public int Id { get; set; }

        public int GroupId { get; set; }

        [Required, MaxLength(200)]
        public string Text { get; set; } = string.Empty;

        public bool Done { get; set; }
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        public TaskGroup? Group { get; set; }
    }
}
