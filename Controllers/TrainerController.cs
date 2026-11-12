using System.Security.Claims;
using GYM.Data;
using GYM.Helpers;
using GYM.Models.QuestModels;
using GYM.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GYM.Controllers
{
    [Authorize(Roles = "Trainer")]
    public class TrainerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TrainerController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        // Loads the current trainer's info for their dashboard
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Dashboard";
            ViewData["Active"] = "dashboard";

            var user = await _context.Users.FindAsync(CurrentUserId);
            if (user == null) return NotFound();

            var model = new TrainerDashboardViewModel
            {
                FullName = user.FullName,
                Specialty = user.Specialty ?? "Not set",
                Description = user.Description ?? "No description added yet",
                AvatarUrl = user.AvatarUrl,
                Lbs = user.Lbs,
                Rank = RankHelper.GetRank(user.Lbs),
                RankImageUrl = RankHelper.GetRankImageUrl(user.Lbs)
            };

            return View("~/Views/User/Trainer/Index.cshtml", model);
        }

        // Loads the current trainer's editable profile fields
        public async Task<IActionResult> EditProfile()
        {
            ViewData["Title"] = "Edit My Trainer Profile";
            ViewData["Active"] = "editprofile";

            var user = await _context.Users.FindAsync(CurrentUserId);
            if (user == null) return NotFound();

            var model = new TrainerProfileEditViewModel
            {
                Specialty = user.Specialty,
                Description = user.Description,
                Lbs = user.Lbs
            };

            return View("~/Views/User/Trainer/EditProfile.cshtml", model);
        }

        // Saves changes to the current trainer's specialty, description and Lbs
        public async Task<IActionResult> UpdateTrainerProfile([FromBody] TrainerProfileEditViewModel model)
        {
            var user = await _context.Users.FindAsync(CurrentUserId);
            if (user == null)
            {
                return Json(new { success = false, errors = new[] { "User not found" } });
            }

            if (!ModelState.IsValid)
            {
                var modelErrors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return Json(new { success = false, errors = modelErrors });
            }

            user.Specialty = model.Specialty;
            user.Description = model.Description;
            user.Lbs = model.Lbs;

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                rank = RankHelper.GetRank(user.Lbs),
                rankImageUrl = RankHelper.GetRankImageUrl(user.Lbs)
            });
        }

        // Loads the current trainer's upcoming available slots for the Availability page
        public async Task<IActionResult> Availability()
        {
            ViewData["Title"] = "Manage Availability";
            ViewData["Active"] = "availability";

            var gymInfo = await _context.GymInfos.FirstOrDefaultAsync();
            ViewBag.OperatingHours = gymInfo?.OperatingHours ?? "Not set";

            var now = DateTime.Now;
            var today = DateTime.Today;
            var currentTime = now.TimeOfDay;

            var slots = await _context.AvailableSlots
                .Where(s => s.TrainerId == CurrentUserId &&
                            (s.Date > today || (s.Date == today && s.StartTime > currentTime)))
                .OrderBy(s => s.Date).ThenBy(s => s.StartTime)
                .Select(s => new AvailableSlotViewModel
                {
                    SlotId = s.SlotId,
                    Date = s.Date,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    IsBooked = s.IsBooked
                })
                .ToListAsync();

            return View("~/Views/User/Trainer/Availability.cshtml", slots);
        }

        // Creates a new available slot after validating date/time and checking for overlaps and gym operating hours
        public async Task<IActionResult> AddSlot([FromBody] AddSlotRequest request)
        {
            if (!ModelState.IsValid)
            {
                var modelErrors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return Json(new { success = false, errors = modelErrors });
            }

            if (!DateTime.TryParse(request.Date, out var date))
            {
                return Json(new { success = false, errors = new[] { "Invalid date" } });
            }

            if (!TimeSpan.TryParse(request.StartTime, out var startTime) ||
                !TimeSpan.TryParse(request.EndTime, out var endTime))
            {
                return Json(new { success = false, errors = new[] { "Invalid time format" } });
            }

            if (date.Date < DateTime.Today)
            {
                return Json(new { success = false, errors = new[] { "Date cannot be in the past" } });
            }

            if (endTime <= startTime)
            {
                return Json(new { success = false, errors = new[] { "End time must be after start time" } });
            }

            // If the slot is for today, the start time must still be in the future
            if (date.Date == DateTime.Today && startTime <= DateTime.Now.TimeOfDay)
            {
                return Json(new { success = false, errors = new[] { "Start time must be in the future" } });
            }
            int dayOfWeek = (int)date.DayOfWeek;
            var (isOpen, openTime, closeTime) = await OperatingHoursHelper.GetHoursForDayAsync(_context, dayOfWeek);

            if (!isOpen)
            {
                return Json(new { success = false, errors = new[] { "The gym is closed on this day" } });
            }

            if (startTime < openTime || endTime > closeTime)
            {
                return Json(new
                {
                    success = false,
                    errors = new[] { $"Slots must be within operating hours for this day ({openTime:hh\\:mm} - {closeTime:hh\\:mm})" }
                });
            }
            bool overlaps = await _context.AvailableSlots.AnyAsync(s =>
                s.TrainerId == CurrentUserId &&
                s.Date.Date == date.Date &&
                startTime < s.EndTime && endTime > s.StartTime);

            if (overlaps)
            {
                return Json(new { success = false, errors = new[] { "This time overlaps with an existing slot" } });
            }

            var slot = new Models.AvailableSlot
            {
                TrainerId = CurrentUserId,
                Date = date.Date,
                StartTime = startTime,
                EndTime = endTime,
                IsBooked = false,
                CreatedAt = DateTime.Now
            };

            _context.AvailableSlots.Add(slot);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                slot = new
                {
                    slot.SlotId,
                    Date = slot.Date.ToString("yyyy-MM-dd"),
                    DateDisplay = slot.Date.ToString("dd MMM yyyy"),
                    StartTime = slot.StartTime.ToString(@"hh\:mm"),
                    EndTime = slot.EndTime.ToString(@"hh\:mm")
                }
            });
        }

        // Deletes an unbooked slot, removing any leftover cancelled appointments tied to it

        [HttpPost]
        public async Task<IActionResult> DeleteSlot([FromBody] SlotIdPayload payload)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Slot not found" });
            }

            var slot = await _context.AvailableSlots.FindAsync(payload.SlotId);
            if (slot == null || slot.TrainerId != CurrentUserId)
            {
                return Json(new { success = false, message = "Slot not found" });
            }

            if (slot.IsBooked)
            {
                return Json(new { success = false, message = "Cannot delete a slot that has already been booked" });
            }

            // Since IsBooked is false, any linked appointments must be Cancelled (no active bookings
            // can exist on an unbooked slot). Remove them first to satisfy the foreign key constraint.
            var relatedAppointments = await _context.Appointments
                .Where(a => a.SlotId == slot.SlotId)
                .ToListAsync();

            if (relatedAppointments.Count > 0)
            {
                _context.Appointments.RemoveRange(relatedAppointments);
            }

            _context.AvailableSlots.Remove(slot);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // Lists the current trainer's appointments, optionally filtered by status, after running cleanup jobs
        public async Task<IActionResult> Appointments(string status = "All")
        {
            ViewData["Title"] = "My Appointments";
            ViewData["Active"] = "appointments";
            ViewBag.SelectedStatus = status;

            await CleanupHelper.RemoveOldCancelledAppointmentsAsync(_context);
            await CleanupHelper.RemoveExpiredPendingAppointmentsAsync(_context);
            await CleanupHelper.CompleteExpiredAppointmentsAsync(_context);

            var query = _context.Appointments
                            .Include(a => a.Member)
                .Include(a => a.Slot)
                .Where(a => a.Slot.TrainerId == CurrentUserId)
                .AsQueryable();

            if (status != "All")
            {
                query = query.Where(a => a.Status == status);
            }

            var appointments = await query
                .OrderBy(a => a.Slot.Date).ThenBy(a => a.Slot.StartTime)
                .Select(a => new TrainerAppointmentViewModel
                {
                    AppointmentId = a.AppointmentId,
                    MemberName = a.Member.FullName,
                    MemberEmail = a.Member.Email,
                    MemberPhone = a.Member.PhoneNumber,
                    Date = a.Slot.Date,
                    StartTime = a.Slot.StartTime,
                    EndTime = a.Slot.EndTime,
                    Status = a.Status,
                    BookedAt = a.BookedAt
                })
                .ToListAsync();

            return View("~/Views/User/Trainer/Appointments.cshtml", appointments);
        }

        // Confirms a pending appointment and notifies the member
        public async Task<IActionResult> ConfirmAppointment([FromBody] AppointmentActionPayload payload)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Appointment not found" });
            }

            var appointment = await _context.Appointments
                .Include(a => a.Slot)
                .FirstOrDefaultAsync(a => a.AppointmentId == payload.AppointmentId);

            if (appointment == null || appointment.Slot.TrainerId != CurrentUserId)
            {
                return Json(new { success = false, message = "Appointment not found" });
            }

            if (appointment.Status != "Pending")
            {
                return Json(new { success = false, message = "Only pending appointments can be confirmed" });
            }

            appointment.Status = "Confirmed";
            appointment.RespondedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            var trainer = await _context.Users.FindAsync(CurrentUserId);
            await NotificationHelper.SendAsync(
                _context,
                appointment.MemberId,
                $"{trainer.FullName} confirmed your appointment on {appointment.Slot.Date:dd MMM} at {appointment.Slot.StartTime:hh\\:mm}.",
                $"/GymRoom/MyBookings?trainerId={appointment.Slot.TrainerId}"
            );

            return Json(new { success = true });
        }

        // Cancels an active appointment, frees up the slot, and notifies the member
        public async Task<IActionResult> CancelAppointment([FromBody] AppointmentActionPayload payload)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Appointment not found" });
            }

            var appointment = await _context.Appointments
                .Include(a => a.Slot)
                .FirstOrDefaultAsync(a => a.AppointmentId == payload.AppointmentId);

            if (appointment == null || appointment.Slot.TrainerId != CurrentUserId)
            {
                return Json(new { success = false, message = "Appointment not found" });
            }

            if (appointment.Status == "Cancelled" || appointment.Status == "Completed")
            {
                return Json(new { success = false, message = "This appointment cannot be cancelled" });
            }

            appointment.Status = "Cancelled";
            appointment.RespondedAt = DateTime.Now;
            appointment.Slot.IsBooked = false;

            await _context.SaveChangesAsync();

            var trainer = await _context.Users.FindAsync(CurrentUserId);
            await NotificationHelper.SendAsync(
                _context,
                appointment.MemberId,
                $"{trainer.FullName} cancelled your appointment on {appointment.Slot.Date:dd MMM} at {appointment.Slot.StartTime:hh\\:mm}.",
                $"/GymRoom/MyBookings?trainerId={appointment.Slot.TrainerId}"
            );

            return Json(new { success = true });
        }

        // Loads members eligible for quests and the trainer's existing assigned quests
        public async Task<IActionResult> AssignQuests()
        {
            ViewData["Title"] = "Assign Quests";
            ViewData["Active"] = "assignquests";

            await CleanupHelper.CompleteExpiredAppointmentsAsync(_context);
            await CleanupHelper.RemoveExpiredTrainerQuestsAsync(_context);

            var today = DateTime.Today;

            var eligibleMembers = await _context.Appointments
                        .Include(a => a.Slot)
                .Include(a => a.Member)
                .Where(a => a.Slot.TrainerId == CurrentUserId &&
                            (a.Status == "Confirmed" || a.Status == "Completed") &&
                            a.Slot.Date >= DateTime.Today)
                .Select(a => new EligibleMemberViewModel
                {
                    MemberId = a.Member.UserId,
                    FullName = a.Member.FullName,
                    Email = a.Member.Email
                })
                .Distinct()
                .ToListAsync();

            ViewBag.EligibleMembers = eligibleMembers;

            var quests = await _context.TrainerQuests
              .Include(q => q.Member)
              .Where(q => q.TrainerId == CurrentUserId)
              .OrderByDescending(q => q.CreatedAt)
              .Select(q => new TrainerQuestListItemViewModel
              {
                  TrainerQuestId = q.TrainerQuestId,
                  MemberName = q.Member.FullName,
                  Title = q.Title,
                  Description = q.Description,
                  ExpReward = q.ExpReward,
                  Status = q.Status,
                  CreatedAt = q.CreatedAt
              })
              .ToListAsync();

            return View("~/Views/User/Trainer/AssignQuests.cshtml", quests);
        }

        // Assigns a new quest to an eligible member and notifies them
        public async Task<IActionResult> CreateTrainerQuest([FromBody] CreateTrainerQuestRequest request)
        {
            if (!ModelState.IsValid)
            {
                var modelErrors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return Json(new { success = false, errors = modelErrors });
            }

            bool isEligible = await _context.Appointments
                .Include(a => a.Slot)
                .AnyAsync(a => a.Slot.TrainerId == CurrentUserId &&
                               a.MemberId == request.MemberId &&
                               (a.Status == "Confirmed" || a.Status == "Completed") &&
                               a.Slot.Date >= DateTime.Today);

            if (!isEligible)
            {
                return Json(new { success = false, errors = new[] { "This member does not have a confirmed booking with you" } });
            }

            var quest = new TrainerQuest
            {
                TrainerId = CurrentUserId,
                MemberId = request.MemberId,
                Title = request.Title,
                Description = request.Description,
                ExpReward = request.ExpReward,
                Status = "InProgress",
                CreatedAt = DateTime.Now
            };

            _context.TrainerQuests.Add(quest);
            await _context.SaveChangesAsync();

            var trainer = await _context.Users.FindAsync(CurrentUserId);
            await NotificationHelper.SendAsync(
                _context,
                request.MemberId,
                $"{trainer.FullName} assigned you a new quest: {request.Title}",
                "/GymRoom/Quest"
            );

            return Json(new { success = true });
        }

        // Deletes a quest the trainer created
        public async Task<IActionResult> DeleteTrainerQuest([FromBody] TrainerQuestIdPayload payload)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Quest not found" });
            }

            var quest = await _context.TrainerQuests.FindAsync(payload.TrainerQuestId);
            if (quest == null || quest.TrainerId != CurrentUserId)
            {
                return Json(new { success = false, message = "Quest not found" });
            }

            _context.TrainerQuests.Remove(quest);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // Loads the current trainer's own profile for the shared Profile page
        public async Task<IActionResult> Profile()
        {
            ViewData["Title"] = "My Profile";
            ViewData["Active"] = "profile";
            ViewData["Layout"] = "~/Views/User/Trainer/_TrainerLayout.cshtml";

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

        // Confirms a member's submitted quest, grants them EXP, and notifies them
        public async Task<IActionResult> ConfirmTrainerQuest([FromBody] TrainerQuestIdPayload payload)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Quest not found" });
            }

            var quest = await _context.TrainerQuests
                .Include(q => q.Member)
                .FirstOrDefaultAsync(q => q.TrainerQuestId == payload.TrainerQuestId);

            if (quest == null || quest.TrainerId != CurrentUserId)
            {
                return Json(new { success = false, message = "Quest not found" });
            }

            if (quest.Status != "PendingReview")
            {
                return Json(new { success = false, message = "This quest is not awaiting review" });
            }

            quest.Status = "Completed";
            quest.CompletedAt = DateTime.Now;

            var member = quest.Member;
            member.Exp += quest.ExpReward;

            await _context.SaveChangesAsync();

            var trainer = await _context.Users.FindAsync(CurrentUserId);
            await NotificationHelper.SendAsync(
                _context,
                quest.MemberId,
                $"{trainer.FullName} confirmed your quest: {quest.Title}. You earned {quest.ExpReward} EXP!",
                "/GymRoom/Quest"
            );

            return Json(new { success = true });
        }
    }
}