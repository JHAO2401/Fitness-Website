namespace GYM.Models.ViewModels
{
    public class GymOperatingHourItemViewModel
    {
        public int DayOfWeek { get; set; }
        public string DayName { get; set; }
        public bool IsClosed { get; set; }
        public string OpenTime { get; set; }
        public string CloseTime { get; set; }
    }

    public class UpdateOperatingHoursRequest
    {
        public List<GymOperatingHourItemViewModel> Days { get; set; } = new();
    }
}