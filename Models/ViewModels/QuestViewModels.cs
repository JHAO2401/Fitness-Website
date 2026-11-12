namespace GYM.Models.ViewModels
{
    public class QuestItemViewModel
    {
        public string Key { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int ExpReward { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class TrainerAssignedQuestViewModel
    {
        public int TrainerQuestId { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public int ExpReward { get; set; }
        public string Status { get; set; }
    }

    public class QuestPageViewModel
    {
        public bool IsLoggedInMember { get; set; }
        public int CurrentExp { get; set; }
        public int Level { get; set; }
        public int ExpIntoLevel { get; set; }
        public int ExpNeededForLevel { get; set; }
        public string Rank { get; set; }
        public string RankImageUrl { get; set; }
        public List<QuestItemViewModel> DailyQuests { get; set; } = new();
        public List<QuestItemViewModel> WeeklyQuests { get; set; } = new();
        public List<TrainerAssignedQuestViewModel> TrainerQuests { get; set; } = new();
    }

    public class CompleteQuestRequest
    {
        public string QuestKey { get; set; }
    }
}