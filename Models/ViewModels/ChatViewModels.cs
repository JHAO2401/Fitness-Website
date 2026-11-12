namespace GYM.Models.ViewModels
{
    public class ChatContactViewModel
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string? AvatarUrl { get; set; }
        public string LastMessage { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public int UnreadCount { get; set; }
    }

    public class ChatMessageViewModel
    {
        public int ChatMessageId { get; set; }
        public int SenderId { get; set; }
        public string Message { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsMine { get; set; }
    }

    public class ChatPageViewModel
    {
        public List<ChatContactViewModel> Contacts { get; set; } = new();
        public int? SelectedContactId { get; set; }
        public string? SelectedContactName { get; set; }
        public string? SelectedContactAvatar { get; set; }
    }

    public class SendMessageRequest
    {
        public int ReceiverId { get; set; }
        public string Message { get; set; }
    }
}