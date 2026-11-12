using GYM.Data;
using GYM.Helpers;
using GYM.Models.ChatModels;
using GYM.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GYM.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ChatController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        // Loads the chat contact list (trainers for Members, members for Trainers) and optionally opens one conversation
        public async Task<IActionResult> Index(int? contactId = null)
        {
            bool isMember = User.IsInRole("Member");
            bool isTrainer = User.IsInRole("Trainer");

            if (!isMember && !isTrainer)
            {
                return RedirectToAction("Login", "Account");
            }

            List<int> contactIds;

            if (isMember)
            {
                // Members can chat with trainers they've booked with
                contactIds = await _context.Appointments
                    .Include(a => a.Slot)
                    .Where(a => a.MemberId == CurrentUserId)
                    .Select(a => a.Slot.TrainerId)
                    .Distinct()
                    .ToListAsync();
            }
            else
            {
                // Trainers can chat with members who've booked with them
                contactIds = await _context.Appointments
                    .Include(a => a.Slot)
                    .Where(a => a.Slot.TrainerId == CurrentUserId)
                    .Select(a => a.MemberId)
                    .Distinct()
                    .ToListAsync();
            }

            var contacts = new List<ChatContactViewModel>();

            foreach (var id in contactIds)
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null) continue;

                var lastMessage = await _context.ChatMessages
                    .Where(m => (m.SenderId == CurrentUserId && m.ReceiverId == id) ||
                                (m.SenderId == id && m.ReceiverId == CurrentUserId))
                    .OrderByDescending(m => m.SentAt)
                    .FirstOrDefaultAsync();

                int unread = await _context.ChatMessages
                    .CountAsync(m => m.SenderId == id && m.ReceiverId == CurrentUserId && !m.IsRead);

                contacts.Add(new ChatContactViewModel
                {
                    UserId = user.UserId,
                    FullName = user.FullName,
                    AvatarUrl = user.AvatarUrl,
                    LastMessage = lastMessage != null
                        ? EncryptionHelper.Decrypt(lastMessage.Message)
                        : "No messages yet",
                    LastMessageAt = lastMessage?.SentAt,
                    UnreadCount = unread
                });
            }

            contacts = contacts.OrderByDescending(c => c.LastMessageAt ?? DateTime.MinValue).ToList();

            var model = new ChatPageViewModel
            {
                Contacts = contacts,
                SelectedContactId = contactId
            };

            if (contactId.HasValue)
            {
                var selectedUser = await _context.Users.FindAsync(contactId.Value);
                if (selectedUser != null && contactIds.Contains(contactId.Value))
                {
                    model.SelectedContactName = selectedUser.FullName;
                    model.SelectedContactAvatar = selectedUser.AvatarUrl;
                }
            }

            ViewData["Title"] = "Chat";

            string layoutPath = isMember
                ? "~/Views/GymRoom/_GymRoomLayout.cshtml"
                : "~/Views/User/Trainer/_TrainerLayout.cshtml";

            if (isMember)
            {
                ViewData["Active"] = "chat";
                return View("~/Views/GymRoom/Chat/Index.cshtml", model);
            }
            else
            {
                ViewData["Active"] = "chat";
                return View("~/Views/User/Trainer/Chat.cshtml", model);
            }
        }

        // Fetches the full message history with a contact and marks incoming messages as read
        [HttpGet]
        public async Task<IActionResult> GetMessages(int contactId)
        {
            bool isAllowed = await IsValidContact(contactId);
            if (!isAllowed)
            {
                return Json(new { success = false, message = "Not authorized to view this conversation" });
            }

            var rawMessages = await _context.ChatMessages
                .Where(m => (m.SenderId == CurrentUserId && m.ReceiverId == contactId) ||
                            (m.SenderId == contactId && m.ReceiverId == CurrentUserId))
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            var messages = rawMessages.Select(m => new ChatMessageViewModel
            {
                ChatMessageId = m.ChatMessageId,
                SenderId = m.SenderId,
                Message = EncryptionHelper.Decrypt(m.Message),
                SentAt = m.SentAt,
                IsMine = m.SenderId == CurrentUserId
            }).ToList();

            var unreadIncoming = await _context.ChatMessages
                .Where(m => m.SenderId == contactId && m.ReceiverId == CurrentUserId && !m.IsRead)
                .ToListAsync();

            foreach (var m in unreadIncoming)
            {
                m.IsRead = true;
            }
            if (unreadIncoming.Count > 0)
            {
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true, messages });
        }

        // Sends a new (encrypted) chat message to an authorized contact
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return Json(new { success = false, message = "Message cannot be empty" });
            }

            bool isAllowed = await IsValidContact(request.ReceiverId);
            if (!isAllowed)
            {
                return Json(new { success = false, message = "Not authorized to message this user" });
            }

            var message = new ChatMessage
            {
                SenderId = CurrentUserId,
                ReceiverId = request.ReceiverId,
                Message = EncryptionHelper.Encrypt(request.Message.Trim()),
                IsRead = false,
                SentAt = DateTime.Now
            };

            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                chatMessageId = message.ChatMessageId,
                sentAt = message.SentAt.ToString("HH:mm")
            });
        }

        // Deletes a message the current user sent
        public async Task<IActionResult> DeleteMessage([FromBody] DeleteMessageRequest request)
        {
            var message = await _context.ChatMessages.FindAsync(request.ChatMessageId);

            if (message == null)
            {
                return Json(new { success = false, message = "Message not found" });
            }

            if (message.SenderId != CurrentUserId)
            {
                return Json(new { success = false, message = "You can only delete your own messages" });
            }

            _context.ChatMessages.Remove(message);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // Verifies the current user is actually allowed to chat with this contact
        // (they have a booking history together)
        private async Task<bool> IsValidContact(int contactId)
        {
            bool isMember = User.IsInRole("Member");

            if (isMember)
            {
                return await _context.Appointments
                    .Include(a => a.Slot)
                    .AnyAsync(a => a.MemberId == CurrentUserId && a.Slot.TrainerId == contactId);
            }
            else
            {
                return await _context.Appointments
                    .Include(a => a.Slot)
                    .AnyAsync(a => a.Slot.TrainerId == CurrentUserId && a.MemberId == contactId);
            }
        }
    }
}