using System.Security.Claims;
using GYM.Data;
using GYM.Helpers;
using GYM.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GYM.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif" };

        public ProfileController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        // Simple health-check endpoint for this controller
        public IActionResult Ping()
        {
            return Content("Profile controller is alive");
        }

        // Updates the current user's name/email/gender and optionally changes their password
        public async Task<IActionResult> UpdateProfile([FromBody] ProfileViewModel model)
        {
            var user = await _context.Users.FindAsync(CurrentUserId);
            if (user == null)
                return Json(new { success = false, errors = new[] { "User not found" } });

            if (!ModelState.IsValid)
            {
                var modelErrors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return Json(new { success = false, errors = modelErrors });
            }

            bool emailTaken = await _context.Users.AnyAsync(u => u.Email == model.Email && u.UserId != user.UserId);
            if (emailTaken)
                return Json(new { success = false, errors = new[] { "This email is already in use" } });

            if (!string.IsNullOrEmpty(model.NewPassword))
            {
                if (string.IsNullOrEmpty(model.CurrentPassword) ||
                    !PasswordHelper.VerifyPassword(model.CurrentPassword, user.PasswordHash, user.PasswordSalt))
                    return Json(new { success = false, errors = new[] { "Current password is incorrect" } });

                if (model.NewPassword != model.ConfirmNewPassword)
                    return Json(new { success = false, errors = new[] { "New passwords do not match" } });

                if (model.NewPassword == model.CurrentPassword)
                    return Json(new { success = false, errors = new[] { "New password must be different from the current password" } });

                string newSalt = PasswordHelper.GenerateSalt();
                user.PasswordSalt = newSalt;
                user.PasswordHash = PasswordHelper.HashPassword(model.NewPassword, newSalt);
            }

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.Gender = model.Gender;

            await _context.SaveChangesAsync();

            return Json(new { success = true, fullName = user.FullName });
        }

        // Uploads/replaces the current user's avatar image after validating type and size
        public async Task<IActionResult> UploadAvatar(IFormFile avatar)
        {
            if (avatar == null || avatar.Length == 0)
                return Json(new { success = false, message = "No file uploaded" });

            var ext = Path.GetExtension(avatar.FileName).ToLowerInvariant();

            if (!AllowedImageExtensions.Contains(ext))
                return Json(new { success = false, message = "Only image files are allowed (jpg, png, gif)" });

            if (avatar.Length > 2 * 1024 * 1024)
                return Json(new { success = false, message = "Image must be under 2MB" });

            var user = await _context.Users.FindAsync(CurrentUserId);
            if (user == null) return Json(new { success = false, message = "User not found" });

            var avatarFolder = Path.Combine(_env.WebRootPath, "image", "avatars");
            Directory.CreateDirectory(avatarFolder);

            foreach (var oldExt in AllowedImageExtensions)
            {
                var oldFile = Path.Combine(avatarFolder, $"user_{user.UserId}{oldExt}");
                if (System.IO.File.Exists(oldFile))
                    System.IO.File.Delete(oldFile);
            }

            var fileName = $"user_{user.UserId}{ext}";
            var filePath = Path.Combine(avatarFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await avatar.CopyToAsync(stream);
            }

            var version = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            user.AvatarUrl = $"/image/avatars/{fileName}?v={version}";
            await _context.SaveChangesAsync();

            return Json(new { success = true, avatarUrl = user.AvatarUrl });
        }
    }
}