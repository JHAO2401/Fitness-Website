namespace GYM.Models.ViewModels
{
    public class NotificationItemViewModel
    {
        public int NotificationId { get; set; }
        public string Message { get; set; }
        public string? LinkUrl { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class NotificationIdPayload
    {
        public int NotificationId { get; set; }
    }
}