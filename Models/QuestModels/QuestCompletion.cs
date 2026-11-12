using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GYM.Models.QuestModels
{
    public class QuestCompletion
    {
        [Key]
        public int QuestCompletionId { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }

        [Required]
        [MaxLength(50)]
        public string QuestKey { get; set; }   // e.g. "daily_login"

        [Required]
        [MaxLength(20)]
        public string PeriodKey { get; set; }  // e.g. "2026-07-25" for daily, "2026-W30" for weekly

        public DateTime CompletedAt { get; set; } = DateTime.Now;
    }
}