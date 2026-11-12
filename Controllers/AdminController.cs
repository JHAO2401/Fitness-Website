using System.Security.Claims;
using GYM.Data;
using GYM.Helpers;
using GYM.Models;
using GYM.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GYM.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif" };

        public AdminController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        // Loads member/trainer counts for the admin dashboard
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Dashboard";
            ViewData["Active"] = "dashboard";

            var model = new AdminDashboardViewModel
            {
                TotalMembers = await _context.Users.CountAsync(u => u.Role == "Member"),
                TotalTrainers = await _context.Users.CountAsync(u => u.Role == "Trainer"),
                ActiveMembers = await _context.Users.CountAsync(u => u.Role == "Member" && u.IsActive),
                ActiveTrainers = await _context.Users.CountAsync(u => u.Role == "Trainer" && u.IsActive)
            };

            return View("~/Views/User/Admin/Index.cshtml", model);
        }

        // Lists all users, optionally filtered by role
        public async Task<IActionResult> ManageUsers(string role = "All")
        {
            ViewData["Title"] = "Manage Users";
            ViewData["Active"] = "users";
            ViewBag.SelectedRole = role;
            ViewBag.CurrentUserId = CurrentUserId;

            var query = _context.Users.AsQueryable();

            if (role != "All")
            {
                query = query.Where(u => u.Role == role);
            }

            var users = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();
            return View("~/Views/User/Admin/ManageUsers.cshtml", users);
        }


        // Resets a locked user's failed login counter
        public async Task<IActionResult> UnlockUser([FromBody] IdPayload payload)
        {
            var user = await _context.Users.FindAsync(payload.UserId);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            user.FailedLoginAttempts = 0;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // Loads the current admin's own profile for the shared Profile page
        public async Task<IActionResult> Profile()
        {
            ViewData["Title"] = "My Profile";
            ViewData["Active"] = "profile";
            ViewData["Layout"] = "~/Views/User/Admin/_AdminLayout.cshtml";

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



        // Lists appointments across all trainers, optionally filtered by status, after running cleanup jobs
        public async Task<IActionResult> ManageAppointments(string status = "All")
        {
            ViewData["Title"] = "Appointments";
            ViewData["Active"] = "appointments";
            ViewBag.SelectedStatus = status;

            await CleanupHelper.RemoveOldCancelledAppointmentsAsync(_context);
            await CleanupHelper.RemoveExpiredPendingAppointmentsAsync(_context);
            await CleanupHelper.CompleteExpiredAppointmentsAsync(_context);

            var query = _context.Appointments
                                    .Include(a => a.Member)
                .Include(a => a.Slot)
                    .ThenInclude(s => s.Trainer)
                .AsQueryable();

            if (status != "All")
            {
                query = query.Where(a => a.Status == status);
            }

            var appointments = await query
                .OrderByDescending(a => a.BookedAt)
                .Select(a => new AppointmentListItemViewModel
                {
                    AppointmentId = a.AppointmentId,
                    MemberName = a.Member.FullName,
                    MemberEmail = a.Member.Email,
                    TrainerName = a.Slot.Trainer.FullName,
                    Date = a.Slot.Date,
                    StartTime = a.Slot.StartTime,
                    EndTime = a.Slot.EndTime,
                    Status = a.Status,
                    BookedAt = a.BookedAt
                })
                .ToListAsync();

            return View("~/Views/User/Admin/ManageAppointments.cshtml", appointments);
        }

        // Deletes a single appointment record
        public async Task<IActionResult> DeleteAppointment([FromBody] AppointmentIdPayload payload)
        {
            var appointment = await _context.Appointments.FindAsync(payload.AppointmentId);
            if (appointment == null)
            {
                return Json(new { success = false, message = "Appointment not found" });
            }

            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // Loads (or seeds default) gym info, equipment, photos and operating hours for the Gym Info page
        public async Task<IActionResult> GymInfo()
        {
            ViewData["Title"] = "Gym Info";
            ViewData["Active"] = "gyminfo";

            var gymInfo = await _context.GymInfos.FirstOrDefaultAsync();
            if (gymInfo == null)
            {
                gymInfo = new GymInfo
                {
                    GymName = "FitZone GYM",
                    Address = "Jalan Bukit Bintang, 55100 Kuala Lumpur, Malaysia",
                    PhoneNumber = "03-1234 5678"
                };
                _context.GymInfos.Add(gymInfo);
                await _context.SaveChangesAsync();
            }

            var equipmentList = await _context.Equipments.OrderByDescending(e => e.CreatedAt).ToListAsync();
            var photoList = await _context.GymPhotos.OrderByDescending(p => p.IsCover).ThenByDescending(p => p.UploadedAt).ToListAsync();

            var operatingHours = await _context.GymOperatingHours.OrderBy(h => h.DayOfWeek).ToListAsync();

            if (operatingHours.Count == 0)
            {
                for (int d = 0; d < 7; d++)
                {
                    operatingHours.Add(new GymOperatingHour
                    {
                        DayOfWeek = d,
                        IsClosed = false,
                        OpenTime = new TimeSpan(6, 0, 0),
                        CloseTime = new TimeSpan(23, 0, 0)
                    });
                }
                _context.GymOperatingHours.AddRange(operatingHours);
                await _context.SaveChangesAsync();
            }

            string[] dayNames = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
            ViewBag.OperatingHours = operatingHours.Select(h => new GymOperatingHourItemViewModel
            {
                DayOfWeek = h.DayOfWeek,
                DayName = dayNames[h.DayOfWeek],
                IsClosed = h.IsClosed,
                OpenTime = h.OpenTime.ToString(@"hh\:mm"),
                CloseTime = h.CloseTime.ToString(@"hh\:mm")
            }).ToList();

            ViewBag.EquipmentList = equipmentList;
            ViewBag.PhotoList = photoList;

            return View("~/Views/User/Admin/GymInfo.cshtml", gymInfo);
        }

        // Updates each day's open/close time and rebuilds the gym's operating-hours summary text
        [HttpPost]
        public async Task<IActionResult> UpdateOperatingHours([FromBody] UpdateOperatingHoursRequest request)
        {
            foreach (var day in request.Days)
            {
                var record = await _context.GymOperatingHours.FirstOrDefaultAsync(h => h.DayOfWeek == day.DayOfWeek);
                if (record == null) continue;

                record.IsClosed = day.IsClosed;

                if (!day.IsClosed)
                {
                    if (!TimeSpan.TryParse(day.OpenTime, out var open) || !TimeSpan.TryParse(day.CloseTime, out var close))
                    {
                        return Json(new { success = false, errors = new[] { $"Invalid time for {day.DayName}" } });
                    }

                    if (close <= open)
                    {
                        return Json(new { success = false, errors = new[] { $"{day.DayName}: closing time must be after opening time" } });
                    }

                    record.OpenTime = open;
                    record.CloseTime = close;
                }
            }

            await _context.SaveChangesAsync();

            var allHours = await _context.GymOperatingHours.OrderBy(h => h.DayOfWeek).ToListAsync();
            string summaryText = OperatingHoursHelper.BuildSummaryText(allHours);

            var gymInfo = await _context.GymInfos.FirstOrDefaultAsync();
            if (gymInfo != null)
            {
                gymInfo.OperatingHours = summaryText;
                gymInfo.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true, summaryText });
        }

        // Updates the gym's basic info (name, address, phone, description)
        [HttpPost]
        public async Task<IActionResult> UpdateGymInfo([FromBody] GymInfo model)
        {
            var gymInfo = await _context.GymInfos.FirstOrDefaultAsync();
            if (gymInfo == null)
            {
                return Json(new { success = false, errors = new[] { "Gym info not found" } });
            }

            if (string.IsNullOrWhiteSpace(model.GymName))
            {
                return Json(new { success = false, errors = new[] { "Gym name is required" } });
            }

            if (string.IsNullOrWhiteSpace(model.Address))
            {
                return Json(new { success = false, errors = new[] { "Address is required" } });
            }

            if (string.IsNullOrWhiteSpace(model.PhoneNumber))
            {
                return Json(new { success = false, errors = new[] { "Phone number is required" } });
            }


            gymInfo.GymName = model.GymName;
            gymInfo.Address = model.Address;
            gymInfo.PhoneNumber = model.PhoneNumber;
            gymInfo.Description = model.Description;
            gymInfo.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // Uploads a new gym gallery photo, marking it as cover if it's the first one
        public async Task<IActionResult> UploadGymPhoto(IFormFile photo)
        {
            if (photo == null || photo.Length == 0)
            {
                return Json(new { success = false, message = "No file uploaded" });
            }

            var ext = Path.GetExtension(photo.FileName).ToLowerInvariant();

            if (!AllowedImageExtensions.Contains(ext))
            {
                return Json(new { success = false, message = "Only image files are allowed (jpg, png, gif)" });
            }

            if (photo.Length > 4 * 1024 * 1024)
            {
                return Json(new { success = false, message = "Image must be under 4MB" });
            }

            var folder = Path.Combine(_env.WebRootPath, "image", "gym");
            Directory.CreateDirectory(folder);

            var fileName = $"gym_{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await photo.CopyToAsync(stream);
            }

            bool isFirstPhoto = !await _context.GymPhotos.AnyAsync();

            var gymPhoto = new GymPhoto
            {
                ImageUrl = $"/image/gym/{fileName}",
                IsCover = isFirstPhoto,
                UploadedAt = DateTime.Now
            };

            _context.GymPhotos.Add(gymPhoto);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                photoId = gymPhoto.GymPhotoId,
                imageUrl = gymPhoto.ImageUrl,
                isCover = gymPhoto.IsCover
            });
        }

        // Sets a gym photo as the gallery cover, unsetting any previous cover
        public async Task<IActionResult> SetCoverPhoto([FromBody] GymPhotoIdPayload payload)
        {
            var photo = await _context.GymPhotos.FindAsync(payload.GymPhotoId);
            if (photo == null)
            {
                return Json(new { success = false, message = "Photo not found" });
            }

            var currentCovers = await _context.GymPhotos.Where(p => p.IsCover).ToListAsync();
            foreach (var p in currentCovers)
            {
                p.IsCover = false;
            }

            photo.IsCover = true;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // Deletes a gym photo (file + DB row), promoting the next photo to cover if needed
        public async Task<IActionResult> DeleteGymPhoto([FromBody] GymPhotoIdPayload payload)
        {
            var photo = await _context.GymPhotos.FindAsync(payload.GymPhotoId);
            if (photo == null)
            {
                return Json(new { success = false, message = "Photo not found" });
            }

            bool wasCover = photo.IsCover;

            // Also remove the physical file from disk, not just the DB row.
            var relativePath = photo.ImageUrl.TrimStart('/');
            var physicalPath = Path.Combine(_env.WebRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(physicalPath))
            {
                System.IO.File.Delete(physicalPath);
            }

            _context.GymPhotos.Remove(photo);
            await _context.SaveChangesAsync();

            if (wasCover)
            {
                var nextPhoto = await _context.GymPhotos.OrderBy(p => p.UploadedAt).FirstOrDefaultAsync();
                if (nextPhoto != null)
                {
                    nextPhoto.IsCover = true;
                    await _context.SaveChangesAsync();
                }
            }

            return Json(new { success = true });
        }

        // Adds a new equipment item after validating name and quantity
        public async Task<IActionResult> AddEquipment([FromBody] Equipment model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                return Json(new { success = false, errors = new[] { "Equipment name is required" } });
            }

            if (model.Quantity < 0)
            {
                return Json(new { success = false, errors = new[] { "Quantity cannot be negative" } });
            }

            var equipment = new Equipment
            {
                Name = model.Name,
                Description = model.Description,
                Quantity = model.Quantity,
                IsAvailable = model.IsAvailable,
                CreatedAt = DateTime.Now
            };

            _context.Equipments.Add(equipment);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                equipment = new
                {
                    equipment.EquipmentId,
                    equipment.Name,
                    equipment.Description,
                    equipment.Quantity,
                    equipment.IsAvailable,
                    equipment.ImageUrl,
                    CreatedAt = equipment.CreatedAt.ToString("dd MMM yyyy")
                }
            });
        }

        // Updates an existing equipment item's details
        public async Task<IActionResult> EditEquipment([FromBody] Equipment model)
        {
            var equipment = await _context.Equipments.FindAsync(model.EquipmentId);
            if (equipment == null)
            {
                return Json(new { success = false, errors = new[] { "Equipment not found" } });
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                return Json(new { success = false, errors = new[] { "Equipment name is required" } });
            }

            if (model.Quantity < 0)
            {
                return Json(new { success = false, errors = new[] { "Quantity cannot be negative" } });
            }

            equipment.Name = model.Name;
            equipment.Description = model.Description;
            equipment.Quantity = model.Quantity;
            equipment.IsAvailable = model.IsAvailable;

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // Deletes an equipment item, cleaning up its image file if present
        public async Task<IActionResult> DeleteEquipment([FromBody] EquipmentIdPayload payload)
        {
            var equipment = await _context.Equipments.FindAsync(payload.EquipmentId);
            if (equipment == null)
            {
                return Json(new { success = false, message = "Equipment not found" });
            }

            // Clean up the equipment's image file, if any, before removing the row.
            if (!string.IsNullOrEmpty(equipment.ImageUrl))
            {
                var relativePath = equipment.ImageUrl.TrimStart('/');
                var physicalPath = Path.Combine(_env.WebRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(physicalPath))
                {
                    System.IO.File.Delete(physicalPath);
                }
            }

            _context.Equipments.Remove(equipment);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // Uploads/replaces an equipment's image after validating file type and size
        public async Task<IActionResult> UploadEquipmentImage(int equipmentId, IFormFile image)
        {
            if (image == null || image.Length == 0)
            {
                return Json(new { success = false, message = "No file uploaded" });
            }

            var ext = Path.GetExtension(image.FileName).ToLowerInvariant();

            if (!AllowedImageExtensions.Contains(ext))
            {
                return Json(new { success = false, message = "Only image files are allowed (jpg, png, gif)" });
            }

            if (image.Length > 2 * 1024 * 1024)
            {
                return Json(new { success = false, message = "Image must be under 2MB" });
            }

            var equipment = await _context.Equipments.FindAsync(equipmentId);
            if (equipment == null)
            {
                return Json(new { success = false, message = "Equipment not found" });
            }

            var folder = Path.Combine(_env.WebRootPath, "image", "equipment");
            Directory.CreateDirectory(folder);

            // Remove any previous image files for this equipment (any extension).
            foreach (var oldExt in AllowedImageExtensions)
            {
                var oldFile = Path.Combine(folder, $"equipment_{equipmentId}{oldExt}");
                if (System.IO.File.Exists(oldFile))
                {
                    System.IO.File.Delete(oldFile);
                }
            }

            var fileName = $"equipment_{equipmentId}{ext}";
            var filePath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            var version = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            equipment.ImageUrl = $"/image/equipment/{fileName}?v={version}";
            await _context.SaveChangesAsync();

            return Json(new { success = true, imageUrl = equipment.ImageUrl });
        }
    }
}