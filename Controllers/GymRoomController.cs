using System.Security.Claims;
using GYM.Data;
using GYM.Helpers;
using GYM.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GYM.Controllers
{
    public class GymRoomController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GymRoomController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Loads gym info, photos, equipment and current open/closed status for the Our Gym page
        public async Task<IActionResult> OurGym()
        {
            ViewData["Title"] = "Our Gym";
            ViewData["Active"] = "ourgym";

            var operatingHours = await _context.GymOperatingHours.OrderBy(h => h.DayOfWeek).ToListAsync();
            var (isOpenNow, statusText) = OperatingHoursHelper.GetCurrentStatus(operatingHours);

            var model = new MemberHomeViewModel
            {
                GymInfo = await _context.GymInfos.FirstOrDefaultAsync(),
                Photos = await _context.GymPhotos.OrderByDescending(p => p.IsCover).ThenByDescending(p => p.UploadedAt).ToListAsync(),
                EquipmentList = await _context.Equipments.Where(e => e.IsAvailable).OrderBy(e => e.Name).ToListAsync(),
                IsOpenNow = isOpenNow,
                TodayStatusText = statusText
            };

            return View("~/Views/GymRoom/OurGym/Index.cshtml", model);
        }

        // Loads the public trainer list with optional gender/search filters, plus the logged-in Member's own trainers
        public async Task<IActionResult> TrainerList(string gender = "All", string search = "")
        {
            ViewData["Title"] = "Trainer List";
            ViewData["Active"] = "trainerlist";
            ViewBag.SelectedGender = gender;
            ViewBag.SearchTerm = search;

            var query = _context.Users.Where(u => u.Role == "Trainer" && u.IsActive);

            if (gender == "Male" || gender == "Female")
            {
                query = query.Where(u => u.Gender == gender);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u => u.FullName.Contains(search) ||
                                          (u.Specialty != null && u.Specialty.Contains(search)));
            }

            var trainers = await query
                .Select(t => new TrainerListItemViewModel
                {
                    TrainerId = t.UserId,
                    FullName = t.FullName,
                    AvatarUrl = t.AvatarUrl,
                    PhoneNumber = t.PhoneNumber,
                    Gender = t.Gender,
                    Specialty = t.Specialty ?? "General Training",
                    Description = t.Description ?? "",
                    Lbs = t.Lbs,
                    Rank = RankHelper.GetRank(t.Lbs),
                    RankImageUrl = RankHelper.GetRankImageUrl(t.Lbs)
                })
                .ToListAsync();

            // Build "Your Trainers" section for logged-in Members
            List<MyTrainerViewModel> myTrainers = new();

            bool isMember = User.Identity != null && User.Identity.IsAuthenticated && User.IsInRole("Member");
            if (isMember)
            {
                int memberId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                var myTrainerIds = await _context.Appointments
                    .Include(a => a.Slot)
                    .Where(a => a.MemberId == memberId && a.Status != "Cancelled")
                    .Select(a => a.Slot.TrainerId)
                    .Distinct()
                    .ToListAsync();

                foreach (var tid in myTrainerIds)
                {
                    var t = await _context.Users.FindAsync(tid);
                    if (t == null) continue;

                    int upcoming = await _context.Appointments
                        .Include(a => a.Slot)
                        .CountAsync(a => a.MemberId == memberId && a.Slot.TrainerId == tid &&
                                          (a.Status == "Pending" || a.Status == "Confirmed") &&
                                          a.Slot.Date >= DateTime.Today);

                    bool hasUnread = await _context.Notifications.AnyAsync(n =>
                        n.UserId == memberId && !n.IsRead &&
                        n.LinkUrl != null && n.LinkUrl.Contains($"trainerId={tid}"));

                    myTrainers.Add(new MyTrainerViewModel
                    {
                        TrainerId = t.UserId,
                        FullName = t.FullName,
                        AvatarUrl = t.AvatarUrl,
                        Specialty = t.Specialty ?? "General Training",
                        Rank = RankHelper.GetRank(t.Lbs),
                        RankImageUrl = RankHelper.GetRankImageUrl(t.Lbs),
                        UpcomingCount = upcoming,
                        HasUnreadUpdate = hasUnread
                    });
                }
            }

            ViewBag.MyTrainers = myTrainers;

            return View("~/Views/GymRoom/TrainerList/Index.cshtml", trainers);
        }

        // Shows a Member's booking history/status with one specific trainer
        public async Task<IActionResult> MyBookings(int trainerId)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated || !User.IsInRole("Member"))
            {
                return RedirectToAction("Login", "Account");
            }

            int memberId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var trainer = await _context.Users.FirstOrDefaultAsync(u => u.UserId == trainerId && u.Role == "Trainer");
            if (trainer == null) return NotFound();

            await CleanupHelper.CompleteExpiredAppointmentsAsync(_context);

            // Mark related notifications as read when viewing this trainer's bookings
            var relatedNotifs = await _context.Notifications
                .Where(n => n.UserId == memberId && !n.IsRead &&
                            n.LinkUrl != null && (n.LinkUrl.EndsWith($"trainerId={trainerId}") || n.LinkUrl.Contains($"trainerId={trainerId}&")))
                .ToListAsync();
            foreach (var n in relatedNotifs) n.IsRead = true;
            if (relatedNotifs.Count > 0) await _context.SaveChangesAsync();

            var bookings = await _context.Appointments
                .Include(a => a.Slot)
                .Where(a => a.MemberId == memberId && a.Slot.TrainerId == trainerId)
                .OrderByDescending(a => a.Slot.Date).ThenByDescending(a => a.Slot.StartTime)
                .Select(a => new MyBookingViewModel
                {
                    AppointmentId = a.AppointmentId,
                    Date = a.Slot.Date,
                    StartTime = a.Slot.StartTime,
                    EndTime = a.Slot.EndTime,
                    Status = a.Status,
                    BookedAt = a.BookedAt
                })
                .ToListAsync();

            ViewData["Title"] = trainer.FullName + " - My Bookings";
            ViewData["Active"] = "trainerlist";
            ViewBag.TrainerName = trainer.FullName;
            ViewBag.TrainerAvatar = trainer.AvatarUrl;

            return View("~/Views/GymRoom/TrainerList/MyBookings.cshtml", bookings);
        }

        // Redirects the old GymRoom Chat route to the dedicated Chat controller
        public async Task<IActionResult> TrainerDetail(int id)
        {
            var trainer = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id && u.Role == "Trainer");
            if (trainer == null) return NotFound();

            var now = DateTime.Now;
            var today = DateTime.Today;
            var currentTime = now.TimeOfDay;

            var slots = await _context.AvailableSlots
                .Where(s => s.TrainerId == id && !s.IsBooked &&
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

            var model = new TrainerDetailViewModel
            {
                TrainerId = trainer.UserId,
                FullName = trainer.FullName,
                AvatarUrl = trainer.AvatarUrl,
                PhoneNumber = trainer.PhoneNumber,
                Specialty = trainer.Specialty ?? "General Training",
                Description = trainer.Description ?? "",
                Lbs = trainer.Lbs,
                Rank = RankHelper.GetRank(trainer.Lbs),
                RankImageUrl = RankHelper.GetRankImageUrl(trainer.Lbs),
                AvailableSlots = slots
            };

            return View("~/Views/GymRoom/TrainerList/Detail.cshtml", model);
        }

        // Redirects the old GymRoom Chat route to the dedicated Chat controller
        public IActionResult Chat()
        {
            return RedirectToAction("Index", "Chat");
        }

        // Builds the Quest page: shows daily/weekly quests to everyone, plus EXP/rank and trainer-assigned quests for logged-in Members
        public async Task<IActionResult> Quest()
        {
            ViewData["Title"] = "Quest";
            ViewData["Active"] = "quest";

            bool isMember = User.Identity != null && User.Identity.IsAuthenticated && User.IsInRole("Member");

            var model = new QuestPageViewModel { IsLoggedInMember = isMember };

            if (isMember)
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int userId = int.Parse(userIdClaim);

                var user = await _context.Users.FindAsync(userId);
                model.CurrentExp = user.Exp;
                model.Level = LevelHelper.GetLevel(user.Exp);
                model.ExpIntoLevel = LevelHelper.GetExpIntoCurrentLevel(user.Exp);
                model.ExpNeededForLevel = LevelHelper.GetExpNeededForNextLevel(user.Exp);
                model.Rank = RankHelper.GetRankByLevel(model.Level);
                model.RankImageUrl = RankHelper.GetRankImageUrlByLevel(model.Level);

                string todayKey = QuestHelper.GetTodayKey();
                string weekKey = QuestHelper.GetThisWeekKey();

                var completedKeys = await _context.QuestCompletions
                    .Where(q => q.UserId == userId && (q.PeriodKey == todayKey || q.PeriodKey == weekKey))
                    .Select(q => q.QuestKey)
                    .ToListAsync();

                model.DailyQuests = QuestHelper.DailyQuests.Select(q => new QuestItemViewModel
                {
                    Key = q.Key,
                    Title = q.Title,
                    Description = q.Description,
                    ExpReward = q.ExpReward,
                    IsCompleted = completedKeys.Contains(q.Key)
                }).ToList();

                model.WeeklyQuests = QuestHelper.WeeklyQuests.Select(q => new QuestItemViewModel
                {
                    Key = q.Key,
                    Title = q.Title,
                    Description = q.Description,
                    ExpReward = q.ExpReward,
                    IsCompleted = completedKeys.Contains(q.Key)
                }).ToList();

                await CleanupHelper.CompleteExpiredAppointmentsAsync(_context);
                await CleanupHelper.RemoveExpiredTrainerQuestsAsync(_context);

                model.TrainerQuests = await _context.TrainerQuests
                .Where(tq => tq.MemberId == userId)
                .OrderByDescending(tq => tq.CreatedAt)
                .Select(tq => new TrainerAssignedQuestViewModel
                {
                    TrainerQuestId = tq.TrainerQuestId,
                    Title = tq.Title,
                    Description = tq.Description,
                    ExpReward = tq.ExpReward,
                    Status = tq.Status
                })
                .ToListAsync();
            }
            else
            {
                model.DailyQuests = QuestHelper.DailyQuests.Select(q => new QuestItemViewModel
                {
                    Key = q.Key,
                    Title = q.Title,
                    Description = q.Description,
                    ExpReward = q.ExpReward
                }).ToList();

                model.WeeklyQuests = QuestHelper.WeeklyQuests.Select(q => new QuestItemViewModel
                {
                    Key = q.Key,
                    Title = q.Title,
                    Description = q.Description,
                    ExpReward = q.ExpReward
                }).ToList();
            }

            return View("~/Views/GymRoom/Quest/Index.cshtml", model);
        }
    }
}