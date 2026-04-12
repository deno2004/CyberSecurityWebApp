using CyberSecurityWebApp.Data;
using CyberSecurityWebApp.Helpers;
using CyberSecurityWebApp.Models;
using CyberSecurityWebApp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CyberSecurityWebApp.Controllers
{
    public class AuthenticationController : Controller
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;

        public AuthenticationController(AppDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public IActionResult Index() => View();

        // ─────────────────────────────────────────
        //  LOGIN
        // ─────────────────────────────────────────
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null || !PasswordHelper.VerifyPassword(password, user.Password))
            {
                ViewBag.Error = "Napačen e-poštni naslov ali geslo.";
                return View();
            }

            // 🔐 Preveri ali je e-pošta potrjena
            if (!user.EmailConfirmed)
            {
                ViewBag.Error = "Prosimo, najprej potrdite vaš e-poštni naslov. Preverite vaš nabiralnik.";
                ViewBag.ShowResend = true;
                ViewBag.UserId = user.UserId;
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.FirstName + " " + user.LastName),
                new Claim(ClaimTypes.Role, user.IsAdmin ? "Admin" : "User")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            return user.IsAdmin
                ? RedirectToAction("Dashboard", "Admin")
                : RedirectToAction("Index", "Home");
        }

        // ─────────────────────────────────────────
        //  REGISTER
        // ─────────────────────────────────────────
        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(User model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Ta e-poštni naslov je že registriran.");
                return View(model);
            }

            if (await _context.Users.AnyAsync(u => u.Username == model.Username))
            {
                ModelState.AddModelError("Username", "To uporabniško ime je že zasedeno.");
                return View(model);
            }

            // Generiraj potrditveni žeton
            var token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
            var expires = DateTime.UtcNow.AddHours(24);

            var newUser = new User
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Username = model.Username,
                Email = model.Email,
                Password = PasswordHelper.HashPassword(model.Password),
                IsAdmin = false,
                EmailConfirmed = false,
                ConfirmationToken = token,
                ConfirmationTokenExpires = expires
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // Pošlji potrditveni e-mail
            var confirmLink = Url.Action(
                "ConfirmEmail", "Authentication",
                new { userId = newUser.UserId, token },
                Request.Scheme)!;

            try
            {
                await _emailService.SendConfirmationEmailAsync(
                    newUser.Email,
                    newUser.FirstName,
                    confirmLink);
            }
            catch (Exception ex)
            {
                // Če pošiljanje ne uspe, še vedno registriraj — logiraj napako
                // V produkciji bi tu logirali napako
                _ = ex;
            }

            TempData["RegisterSuccess"] = newUser.Email;
            return RedirectToAction("RegisterSuccess");
        }

        // ─────────────────────────────────────────
        //  REGISTER SUCCESS (info stran)
        // ─────────────────────────────────────────
        [HttpGet]
        public IActionResult RegisterSuccess()
        {
            ViewBag.Email = TempData["RegisterSuccess"]?.ToString();
            return View();
        }

        // ─────────────────────────────────────────
        //  CONFIRM EMAIL
        // ─────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(int userId, string token)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                ViewBag.Status = "error";
                ViewBag.Message = "Uporabnik ne obstaja.";
                return View();
            }

            if (user.EmailConfirmed)
            {
                ViewBag.Status = "already";
                ViewBag.Message = "Vaš e-poštni naslov je bil že potrjen.";
                return View();
            }

            if (user.ConfirmationToken != token)
            {
                ViewBag.Status = "error";
                ViewBag.Message = "Neveljavna potrditvena povezava.";
                return View();
            }

            if (user.ConfirmationTokenExpires < DateTime.UtcNow)
            {
                ViewBag.Status = "expired";
                ViewBag.Message = "Potrditvena povezava je potekla. Zahtevajte novo.";
                ViewBag.UserId = userId;
                return View();
            }

            // Potrdi
            user.EmailConfirmed = true;
            user.ConfirmationToken = null;
            user.ConfirmationTokenExpires = null;
            await _context.SaveChangesAsync();

            ViewBag.Status = "success";
            ViewBag.Message = "E-poštni naslov je bil uspešno potrjen!";
            ViewBag.Name = user.FirstName;
            return View();
        }

        // ─────────────────────────────────────────
        //  RESEND CONFIRMATION EMAIL
        // ─────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> ResendConfirmation(int userId)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null || user.EmailConfirmed)
            {
                TempData["Error"] = "Zahteva ni veljavna.";
                return RedirectToAction("Login");
            }

            var token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
            var expires = DateTime.UtcNow.AddHours(24);

            user.ConfirmationToken = token;
            user.ConfirmationTokenExpires = expires;
            await _context.SaveChangesAsync();

            var confirmLink = Url.Action(
                "ConfirmEmail", "Authentication",
                new { userId = user.UserId, token },
                Request.Scheme)!;

            try
            {
                await _emailService.SendConfirmationEmailAsync(
                    user.Email, user.FirstName, confirmLink);

                TempData["Success"] = "Potrditveni e-mail je bil znova poslan.";
            }
            catch
            {
                TempData["Error"] = "Pošiljanje e-maila ni uspelo. Poskusite znova.";
            }

            return RedirectToAction("Login");
        }

        // ─────────────────────────────────────────
        //  LOGOUT
        // ─────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}