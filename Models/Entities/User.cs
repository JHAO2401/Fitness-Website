using System.ComponentModel.DataAnnotations;

namespace GYM.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [MaxLength(100)]
        public string Email { get; set; }

        [Required]
        [MaxLength(256)]
        public string PasswordHash { get; set; }

        [Required]
        [MaxLength(256)]
        public string PasswordSalt { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [MaxLength(100)]
        public string FullName { get; set; }

        [Phone(ErrorMessage = "Invalid phone number format")]
        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [Required]
        [MaxLength(20)]
        public string Role { get; set; }   // "Admin" / "Trainer" / "Member"

        [MaxLength(10)]
        public string? Gender { get; set; }
        public bool IsActive { get; set; } = true;

        public int FailedLoginAttempts { get; set; } = 0;

        public DateTime? LockoutEnd { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [MaxLength(300)]
        public string? AvatarUrl { get; set; }

        // Trainer-specific fields
        [MaxLength(200)]
        public string? Specialty { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public int Lbs { get; set; } = 0;

        // Quest / EXP system
        public int Exp { get; set; } = 0;
    }
}