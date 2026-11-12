using System.ComponentModel.DataAnnotations;

namespace GYM.Models
{
    public class GymPhoto
    {
        [Key]
        public int GymPhotoId { get; set; }

        [Required]
        [MaxLength(300)]
        public string ImageUrl { get; set; }

        public bool IsCover { get; set; } = false;

        public DateTime UploadedAt { get; set; } = DateTime.Now;
    }
}