using System.ComponentModel.DataAnnotations;

namespace GYM.Models
{
    public class Equipment
    {
        [Key]
        public int EquipmentId { get; set; }

        [Required(ErrorMessage = "Equipment name is required")]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Range(0, 999, ErrorMessage = "Quantity must be a valid number")]
        public int Quantity { get; set; } = 1;

        [MaxLength(300)]
        public string? ImageUrl { get; set; }

        public bool IsAvailable { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}