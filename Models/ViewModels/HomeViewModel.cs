namespace GYM.Models.ViewModels
{
    public class HomeViewModel
    {
        public int TrainerCount { get; set; }
        public int MemberCount { get; set; }
        public int LocationCount { get; set; } = 1;
    }

    public class MemberHomeViewModel
    {
        public GymInfo GymInfo { get; set; }
        public List<GymPhoto> Photos { get; set; } = new();
        public List<Equipment> EquipmentList { get; set; } = new();

        public bool IsOpenNow { get; set; }
        public string TodayStatusText { get; set; } = "Closed";
    }
}