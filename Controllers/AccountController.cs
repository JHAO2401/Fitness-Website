using System.Security.Claims;
using GYM.Data;
using GYM.Helpers;
using GYM.Models;
using GYM.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GYM.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Shows the login page
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // Shows the registration page
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // Validates credentials, locks account after 3 failed attempts, and signs the user in via cookie auth
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return Json(new { success = false, errors });
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null)
            {
                return Json(new { success = false, errors = new[] { "Invalid email or password" } });
            }

            if (user.FailedLoginAttempts >= 3)
            {
                return Json(new
                {
                    success = false,
                    errors = new[] { "Your account has been locked due to 3 failed login attempts. Please contact the administrator to unlock it." }
                });
            }

            bool isValid = PasswordHelper.VerifyPassword(model.Password, user.PasswordHash, user.PasswordSalt);

            if (!isValid)
            {
                user.FailedLoginAttempts++;
                await _context.SaveChangesAsync();

                if (user.FailedLoginAttempts >= 3)
                {
                    return Json(new
                    {
                        success = false,
                        errors = new[] { "Your account has been locked due to 3 failed login attempts. Please contact the administrator to unlock it." }
                    });
                }

                return Json(new { success = false, errors = new[] { "Invalid email or password" } });
            }

            if (!user.IsActive)
            {
                return Json(new { success = false, errors = new[] { "This account has been deactivated. Please contact the administrator" } });
            }

            user.FailedLoginAttempts = 0;
            await _context.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            string redirectUrl;
            if (!string.IsNullOrEmpty(model.ReturnUrl) && user.Role == "Member")
            {
                redirectUrl = model.ReturnUrl;
            }
            else
            {
                redirectUrl = user.Role switch
                {
                    "Admin" => Url.Action("Index", "Admin"),
                    "Trainer" => Url.Action("Index", "Trainer"),
                    "Member" => Url.Action("OurGym", "GymRoom"),
                    _ => Url.Action("Login")
                };
            }

            return Json(new { success = true, redirectUrl });
        }

        // Signs the current user out and redirects to Login
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        // Creates a new Member or Trainer account after validating email/phone uniqueness
        public async Task<IActionResult> Register([FromBody] RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return Json(new { success = false, errors });
            }

            if (model.Role != "Member" && model.Role != "Trainer")
            {
                return Json(new { success = false, errors = new[] { "Invalid role selected" } });
            }

            bool emailExists = await _context.Users.AnyAsync(u => u.Email == model.Email);
            if (emailExists)
            {
                return Json(new { success = false, errors = new[] { "This email is already registered" } });
            }

            bool phoneExists = await _context.Users.AnyAsync(u => u.PhoneNumber == model.PhoneNumber);
            if (phoneExists)
            {
                return Json(new { success = false, errors = new[] { "This phone number is already registered" } });
            }

            string salt = PasswordHelper.GenerateSalt();
            string hash = PasswordHelper.HashPassword(model.Password, salt);

            var newUser = new User
            {
                FullName = model.FullName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                Gender = model.Gender,
                PasswordHash = hash,
                PasswordSalt = salt,
                Role = model.Role,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }


        // Shows the admin registration page
        [HttpGet]
        public IActionResult AdminRegister()
        {
            return View();
        }

        // Creates a new Admin account after validating email uniqueness
        public async Task<IActionResult> AdminRegister([FromBody] RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return Json(new { success = false, errors });
            }

            bool emailExists = await _context.Users.AnyAsync(u => u.Email == model.Email);
            if (emailExists)
            {
                return Json(new { success = false, errors = new[] { "This email is already registered" } });
            }

            string salt = PasswordHelper.GenerateSalt();
            string hash = PasswordHelper.HashPassword(model.Password, salt);

            var newAdmin = new User
            {
                FullName = model.FullName,
                Email = model.Email,
                PhoneNumber = null,
                PasswordHash = hash,
                PasswordSalt = salt,
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(newAdmin);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // Shows the Access Denied page
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}