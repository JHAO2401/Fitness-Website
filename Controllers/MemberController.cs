using System.Security.Claims;
using GYM.Data;
using GYM.Helpers;
using GYM.Models;
using GYM.Models.QuestModels;
using GYM.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GYM.Controllers
{
    [Authorize(Roles = "Member")]
    public class MemberController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MemberController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        // Books an available slot for the current Member, enforcing the 3-trainer limit and no time-overlap rules
        [HttpPost]
        public async Task<IActionResult> BookAppointment([FromBody] BookAppointmentRequest request)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "This slot no longer exists" });
            }

            var slot = await _context.AvailableSlots.FindAsync(request.SlotId);

            if (slot == null)
            {
                return Json(new { success = false, message = "This slot no longer exists" });
            }

            if (slot.IsBooked)
            {
                return Json(new { success = false, message = "This slot has already been booked" });
            }

            if (slot.Date < DateTime.Today ||
               (slot.Date == DateTime.Today && slot.StartTime <= DateTime.Now.TimeOfDay))
            {
                return Json(new { success = false, message = "This slot is in the past" });
            }

            // Active bookings = appointments that are still ongoing (not cancelled)
            var activeAppointments = await _context.Appointments
                .Include(a => a.Slot)
                .Where(a => a.MemberId == CurrentUserId && a.Status != "Cancelled")
                .ToListAsync();

            // Limit: a member can only have active bookings with up to 3 different trainers at a time
            var distinctTrainerIds = activeAppointments.Select(a => a.Slot.TrainerId).Distinct().ToHashSet();
            if (!distinctTrainerIds.Contains(slot.TrainerId) && distinctTrainerIds.Count >= 3)
            {
                return Json(new { success = false, message = "You can only have active bookings with up to 3 different trainers at a time." });
            }

            // Prevent double-booking: the member cannot have another active appointment that overlaps this time
            bool hasTimeConflict = activeAppointments.Any(a =>
                a.Slot.Date.Date == slot.Date.Date &&
                slot.StartTime < a.Slot.EndTime && slot.EndTime > a.Slot.StartTime);

            if (hasTimeConflict)
            {
                return Json(new { success = false, message = "This time overlaps with another appointment you already have." });
            }

            slot.IsBooked = true;

            var appointment = new Appointment
            {
                SlotId = slot.SlotId,
                MemberId = CurrentUserId,
                Status = "Pending",
                BookedAt = DateTime.Now
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            var member = await _context.Users.FindAsync(CurrentUserId);
            await NotificationHelper.SendAsync(
                _context,
                slot.TrainerId,
                $"{member.FullName} booked an appointment with you on {slot.Date:dd MMM} at {slot.StartTime:hh\\:mm}.",
                "/Trainer/Appointments"
            );

            return Json(new { success = true });
        }

        // Marks a daily/weekly quest as completed for the current period and grants EXP
        public async Task<IActionResult> CompleteQuest([FromBody] CompleteQuestRequest request)
        {
            var quest = QuestHelper.FindQuest(request.QuestKey);
            if (quest == null)
            {
                return Json(new { success = false, message = "Quest not found" });
            }

            string periodKey = quest.Type == "Daily" ? QuestHelper.GetTodayKey() : QuestHelper.GetThisWeekKey();

            bool alreadyCompleted = await _context.QuestCompletions.AnyAsync(q =>
                q.UserId == CurrentUserId && q.QuestKey == quest.Key && q.PeriodKey == periodKey);

            if (alreadyCompleted)
            {
                return Json(new { success = false, message = "Quest already completed for this period" });
            }

            var completion = new QuestCompletion
            {
                UserId = CurrentUserId,
                QuestKey = quest.Key,
                PeriodKey = periodKey,
                CompletedAt = DateTime.Now
            };
            _context.QuestCompletions.Add(completion);

            var user = await _context.Users.FindAsync(CurrentUserId);
            user.Exp += quest.ExpReward;

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                newExp = user.Exp,
                newLevel = LevelHelper.GetLevel(user.Exp),
                expIntoLevel = LevelHelper.GetExpIntoCurrentLevel(user.Exp),
                expNeeded = LevelHelper.GetExpNeededForNextLevel(user.Exp)
            });
        }

        // Submits a trainer-assigned quest for the trainer's review, if the member is still eligible
        public async Task<IActionResult> CompleteTrainerQuest([FromBody] TrainerQuestIdPayload payload)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Quest not found" });
            }

            var quest = await _context.TrainerQuests.FindAsync(payload.TrainerQuestId);

            if (quest == null || quest.MemberId != CurrentUserId)
            {
                return Json(new { success = false, message = "Quest not found" });
            }

            if (quest.Status != "InProgress")
            {
                return Json(new { success = false, message = "This quest has already been submitted or completed" });
            }

            bool stillEligible = await _context.Appointments
                .Include(a => a.Slot)
                .AnyAsync(a => a.Slot.TrainerId == quest.TrainerId &&
                               a.MemberId == quest.MemberId &&
                               (a.Status == "Confirmed" || a.Status == "Completed") &&
                               a.Slot.Date >= DateTime.Today);

            if (!stillEligible)
            {
                return Json(new { success = false, message = "This quest is no longer available because the related booking has expired" });
            }

            quest.Status = "PendingReview";
            quest.SubmittedAt = DateTime.Now;

            var user = await _context.Users.FindAsync(CurrentUserId);

            await _context.SaveChangesAsync();

            await NotificationHelper.SendAsync(
                _context,
                quest.TrainerId,
                $"{user.FullName} submitted the quest for review: {quest.Title}",
                "/Trainer/AssignQuests"
            );

            return Json(new { success = true });
        }

        // Loads the current Member's own profile for the shared Profile page
        public async Task<IActionResult> Profile()
        {
            ViewData["Title"] = "My Profile";
            ViewData["Active"] = "profile";
            ViewData["Layout"] = "~/Views/GymRoom/_GymRoomLayout.cshtml";

            var user = await _context.Users.FindAsync(CurrentUserId);
            if (user == null) return NotFound();

            var model = new ProfileViewModel
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Gender = user.Gender,
                AvatarUrl = user.AvatarUrl
            };

            return View("~/Views/User/Profile.cshtml", model);
        }
    }
}