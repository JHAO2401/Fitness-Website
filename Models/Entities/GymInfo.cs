using System.ComponentModel.DataAnnotations;

namespace GYM.Models
{
    public class GymInfo
    {
        [Key]
        public int GymInfoId { get; set; }

        [Required(ErrorMessage = "Gym name is required")]
        [MaxLength(100)]
        public string GymName { get; set; } = "FitZone GYM";

        [Required(ErrorMessage = "Address is required")]
        [MaxLength(300)]
        public string Address { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [MaxLength(20)]
        public string PhoneNumber { get; set; }

        [MaxLength(300)]
        public string? OperatingHours { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        [MaxLength(300)]
        public string? CoverImageUrl { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}