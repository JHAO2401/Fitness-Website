using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GYM.Models.QuestModels
{
    public class TrainerQuest
    {
        [Key]
        public int TrainerQuestId { get; set; }

        [Required]
        public int TrainerId { get; set; }

        [ForeignKey("TrainerId")]
        public User Trainer { get; set; }

        [Required]
        public int MemberId { get; set; }

        [ForeignKey("MemberId")]
        public User Member { get; set; }

        [Required]
        [MaxLength(100)]
        public string Title { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Range(1, 500)]
        public int ExpReward { get; set; } = 20;

        [MaxLength(20)]
        public string Status { get; set; } = "InProgress"; // InProgress / PendingReview / Completed

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? SubmittedAt { get; set; }   // when Member marks it done
        public DateTime? CompletedAt { get; set; }   // when Trainer confirms it
    }
}