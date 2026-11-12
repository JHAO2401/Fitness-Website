using System.ComponentModel.DataAnnotations;

namespace GYM.Models.ViewModels
{
    public class AppointmentListItemViewModel
    {
        public int AppointmentId { get; set; }
        public string MemberName { get; set; }
        public string MemberEmail { get; set; }
        public string TrainerName { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Status { get; set; }
        public DateTime BookedAt { get; set; }
    }

    public class TrainerAppointmentViewModel
    {
        public int AppointmentId { get; set; }
        public string MemberName { get; set; }
        public string MemberEmail { get; set; }
        public string? MemberPhone { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Status { get; set; }
        public DateTime BookedAt { get; set; }
    }

    public class AppointmentActionPayload
    {
        [Range(1, int.MaxValue, ErrorMessage = "Invalid appointment")]
        public int AppointmentId { get; set; }
    }

    public class AvailableSlotViewModel
    {
        public int SlotId { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsBooked { get; set; }
    }

    public class AddSlotRequest
    {
        [Required(ErrorMessage = "Date is required")]
        public string Date { get; set; }

        [Required(ErrorMessage = "Start time is required")]
        public string StartTime { get; set; }

        [Required(ErrorMessage = "End time is required")]
        public string EndTime { get; set; }
    }

    public class SlotIdPayload
    {
        [Range(1, int.MaxValue, ErrorMessage = "Invalid slot")]
        public int SlotId { get; set; }
    }

    public class BookAppointmentRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "Invalid slot")]
        public int SlotId { get; set; }
    }
}