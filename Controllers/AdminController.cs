using CyberSecurityWebApp.Data;
using CyberSecurityWebApp.Helpers;
using CyberSecurityWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CyberSecurityWebApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // -------- ADMIN DASHBOARD --------
        public IActionResult Dashboard()
        {
            return View();
        }

        public async Task<IActionResult> Users()
        {
            return View(await _context.Users.ToListAsync());
        }

        public IActionResult AddUser()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUser(User model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // 🔐 HASH PASSWORD
            model.Password = PasswordHelper.HashPassword(model.Password);

            _context.Users.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Uporabnik uspešno dodan.";
            return RedirectToAction("Users");
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
                return NotFound();

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(User model)
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                TempData["Error"] = "Uporabnik ne obstaja.";
                return RedirectToAction("Users");
            }

            // 🔐 Trenutni prijavljeni uporabnik
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // ❌ Admin ne more izbrisati samega sebe
            if (user.UserId.ToString() == currentUserId)
            {
                TempData["Error"] = "Ne moreš izbrisati svojega računa.";
                return RedirectToAction("Users");
            }

            // 🛡️ Prepreči brisanje zadnjega admina
            if (user.IsAdmin)
            {
                var adminCount = await _context.Users.CountAsync(u => u.IsAdmin);

                if (adminCount <= 1)
                {
                    TempData["Error"] = "Sistem mora imeti vsaj enega administratorja.";
                    return RedirectToAction("Users");
                }
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Uporabnik uspešno izbrisan.";
            return RedirectToAction("Users");
        }


    }
}
