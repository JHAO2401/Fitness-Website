using System.ComponentModel.DataAnnotations;

namespace GYM.Models.ViewModels
{
    public class ProfileViewModel
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [MaxLength(100)]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [MaxLength(100)]
        public string Email { get; set; }

        [MaxLength(10)]
        public string? Gender { get; set; }

        public string? AvatarUrl { get; set; }

        [DataType(DataType.Password)]
        public string? CurrentPassword { get; set; }

        [DataType(DataType.Password)]
        [MaxLength(256)]
        public string? NewPassword { get; set; }

        [Compare("NewPassword", ErrorMessage = "New passwords do not match")]
        [DataType(DataType.Password)]
        public string? ConfirmNewPassword { get; set; }
    }
}