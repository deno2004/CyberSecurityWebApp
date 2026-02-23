using CyberSecurityWebApp.Data;
using CyberSecurityWebApp.Helpers;
using CyberSecurityWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public IActionResult Learn()
        {
            return View();
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return RedirectToAction("Login", "Authentication");

            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null)
                return NotFound();

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(User model)
        {
            var user = await _context.Users.FindAsync(model.UserId);

            if (user == null)
                return NotFound();

            // 🔐 DOBI ID trenutno prijavljenega uporabnika
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 🔒 PREPREČI, da si admin odstrani admin pravice
            if (user.UserId.ToString() == currentUserId && !model.IsAdmin)
            {
                ModelState.AddModelError("", "Ne moreš si odstraniti admin pravic.");
                return View(model);
            }

            // POSODOBI PODATKE
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Username = model.Username;
            user.Email = model.Email;
            user.IsAdmin = model.IsAdmin;

            // 🔐 Če je vneseno novo geslo → hash
            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                user.Password = PasswordHelper.HashPassword(model.Password);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Uporabnik uspešno posodobljen.";
            return RedirectToAction("Users");
        }
    }
}
