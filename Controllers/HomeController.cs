using CyberSecurityWebApp.Data;
using CyberSecurityWebApp.Helpers;
using CyberSecurityWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

namespace CyberSecurityWebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public HomeController(ILogger<HomeController> logger, AppDbContext context, IWebHostEnvironment env)
        {
            _logger = logger;
            _context = context;
            _env = env;
        }

        public IActionResult Learn() => View();

        public IActionResult About() => View();

        public IActionResult Index() => View();
        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() =>
            View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

        // ─────────────────────────────────────────
        //  PROFILE GET
        // ─────────────────────────────────────────
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdStr == null)
                return RedirectToAction("Login", "Authentication");

            int userId = int.Parse(userIdStr);

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            // ✅ Vsi kvizi v sistemu
            var allQuizzes = await _context.Quizzes.ToListAsync();

            // ✅ Kvizi, ki jih je uporabnik opravil (ima certifikat)
            var completions = await _context.QuizCompletions
                .Where(c => c.UserId == userId)
                .ToListAsync();

            // ✅ Sestavi seznam z statusom za vsak kviz
            var quizStatuses = allQuizzes.Select(q =>
            {
                var comp = completions.FirstOrDefault(c => c.QuizId == q.QuizId);
                return new
                {
                    Quiz = q,
                    Completed = comp != null,
                    Score = comp?.Score ?? 0,
                    Total = comp?.Total ?? 0,
                    Percentage = comp?.Percentage ?? 0.0,
                    CompletedAt = comp?.CompletedAt
                };
            }).ToList();

            ViewBag.QuizStatuses = quizStatuses;
            ViewBag.CompletedCount = completions.Count;
            ViewBag.TotalQuizzesCount = allQuizzes.Count;

            return View(user);
        }

        // ─────────────────────────────────────────
        //  PROFILE POST (update)
        // ─────────────────────────────────────────
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdateProfile(User model)
        {
            var user = await _context.Users.FindAsync(model.UserId);
            if (user == null) return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (user.UserId.ToString() == currentUserId && !model.IsAdmin)
            {
                ModelState.AddModelError("", "Ne moreš si odstraniti admin pravic.");
                return View("Profile", model);
            }

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Username = model.Username;
            user.Email = model.Email;

            if (!string.IsNullOrWhiteSpace(model.Password))
                user.Password = PasswordHelper.HashPassword(model.Password);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Profil uspešno posodobljen.";
            return RedirectToAction("Profile");
        }
    }
}