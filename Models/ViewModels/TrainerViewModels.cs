using System.ComponentModel.DataAnnotations;

namespace GYM.Models.ViewModels
{
    public class TrainerDashboardViewModel
    {
        public string FullName { get; set; }
        public string Specialty { get; set; }
        public string Description { get; set; }
        public string? AvatarUrl { get; set; }
        public int Lbs { get; set; }
        public string Rank { get; set; }
        public string RankImageUrl { get; set; }
    }

    public class TrainerProfileEditViewModel
    {
        [MaxLength(200)]
        public string? Specialty { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Lbs cannot be negative")]
        public int Lbs { get; set; }
    }

    public class TrainerListItemViewModel
    {
        public int TrainerId { get; set; }
        public string FullName { get; set; }
        public string? AvatarUrl { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Gender { get; set; }
        public string Specialty { get; set; }
        public string Description { get; set; }
        public int Lbs { get; set; }
        public string Rank { get; set; }
        public string RankImageUrl { get; set; }
    }

    public class TrainerDetailViewModel : TrainerListItemViewModel
    {
        public List<AvailableSlotViewModel> AvailableSlots { get; set; } = new();
    }

    public class EligibleMemberViewModel
    {
        public int MemberId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
    }

    public class TrainerQuestListItemViewModel
    {
        public int TrainerQuestId { get; set; }
        public string MemberName { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public int ExpReward { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateTrainerQuestRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "A valid member must be selected")]
        public int MemberId { get; set; }

        [Required(ErrorMessage = "Quest title is required")]
        [MaxLength(200)]
        public string Title { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "EXP reward must be greater than 0")]
        public int ExpReward { get; set; }
    }

    public class TrainerQuestIdPayload
    {
        [Range(1, int.MaxValue, ErrorMessage = "Invalid quest")]
        public int TrainerQuestId { get; set; }
    }

    public class MyTrainerViewModel
    {
        public int TrainerId { get; set; }
        public string FullName { get; set; }
        public string? AvatarUrl { get; set; }
        public string Specialty { get; set; }
        public string Rank { get; set; }
        public string RankImageUrl { get; set; }
        public int UpcomingCount { get; set; }
        public bool HasUnreadUpdate { get; set; }
    }

    public class MyBookingViewModel
    {
        public int AppointmentId { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Status { get; set; }
        public DateTime BookedAt { get; set; }
    }
}