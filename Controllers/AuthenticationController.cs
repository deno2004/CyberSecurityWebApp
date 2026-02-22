using CyberSecurityWebApp.Data;
using CyberSecurityWebApp.Helpers;
using CyberSecurityWebApp.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CyberSecurityWebApp.Controllers
{
    public class AuthenticationController : Controller
    {
        private readonly AppDbContext _context;

        public AuthenticationController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                ViewBag.Error = "Invalid email or password.";
                return View();
            }

            // 🔐 VERIFY HASHED PASSWORD
            bool isValidPassword = PasswordHelper.VerifyPassword(password, user.Password);

            if (!isValidPassword)
            {
                ViewBag.Error = "Invalid email or password.";
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.FirstName + " " + user.LastName),
                new Claim(ClaimTypes.Role, user.IsAdmin ? "Admin" : "User")
            };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity));

            if (user.IsAdmin)
                return RedirectToAction("Dashboard", "Admin");

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(string firstname, string lastname, string username, string email, string password)
        {
            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                ViewBag.Error = "Email already registered.";
                return View();
            }

            var newUser = new User
            {
                FirstName = firstname,
                LastName = lastname,
                Username = username,
                Email = email,

                // 🔐 HASH PASSWORD
                Password = PasswordHelper.HashPassword(password),

                IsAdmin = false
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return RedirectToAction("Login");
        }


        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}
