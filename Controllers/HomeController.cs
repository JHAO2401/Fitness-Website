using GYM.Data;
using GYM.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GYM.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Loads active trainer/member counts for the public landing page
        public async Task<IActionResult> Index()
        {
            var model = new HomeViewModel
            {
                TrainerCount = await _context.Users.CountAsync(u => u.Role == "Trainer" && u.IsActive),
                MemberCount = await _context.Users.CountAsync(u => u.Role == "Member" && u.IsActive),
                LocationCount = 1
            };

            return View(model);
        }
    }
}