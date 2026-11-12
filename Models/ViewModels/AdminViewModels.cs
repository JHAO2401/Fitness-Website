namespace GYM.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalMembers { get; set; }
        public int TotalTrainers { get; set; }
        public int ActiveMembers { get; set; }
        public int ActiveTrainers { get; set; }
    }

    public class IdPayload
    {
        public int UserId { get; set; }
    }

    public class AppointmentIdPayload
    {
        public int AppointmentId { get; set; }
    }

    public class EquipmentIdPayload
    {
        public int EquipmentId { get; set; }
    }

    public class GymPhotoIdPayload
    {
        public int GymPhotoId { get; set; }
    }
}