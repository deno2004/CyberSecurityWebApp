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

        public async Task<IActionResult> Quizzes()
        {
            var quizzes = await _context.Quizzes.ToListAsync();
            return View(quizzes);
        }

        public IActionResult AddQuiz()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddQuiz(Quiz quiz)
        {
            _context.Quizzes.Add(quiz);
            await _context.SaveChangesAsync();

            return RedirectToAction("Quizzes");
        }

        public async Task<IActionResult> EditQuiz(int id)
        {
            var quiz = await _context.Quizzes.FindAsync(id);
            if (quiz == null)
                return NotFound();

            return View(quiz);
        }

        [HttpPost]
        public async Task<IActionResult> EditQuiz(Quiz model)
        {
            var quiz = await _context.Quizzes.FindAsync(model.QuizId);
            if (quiz == null)
                return NotFound();

            quiz.Title = model.Title;
            quiz.Description = model.Description;

            await _context.SaveChangesAsync();

            return RedirectToAction("Quizzes");
        }

        public async Task<IActionResult> Questions(int id)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                    .ThenInclude(q => q.Answers)
                .FirstOrDefaultAsync(q => q.QuizId == id);

            if (quiz == null)
                return NotFound();

            return View(quiz);
        }

        public IActionResult AddQuestion(int quizId)
        {
            ViewBag.QuizId = quizId;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddQuestion(
            int quizId,
            string questionText,
            List<string> answers,
            int correctAnswerIndex)
        {
            var question = new Question
            {
                QuizId = quizId,
                Text = questionText,
                Answers = new List<Answer>()
            };

            for (int i = 0; i < answers.Count; i++)
            {
                question.Answers.Add(new Answer
                {
                    Text = answers[i],
                    IsCorrect = i == correctAnswerIndex
                });
            }

            _context.Questions.Add(question);
            await _context.SaveChangesAsync();

            return RedirectToAction("Questions", new { id = quizId });
        }

        public async Task<IActionResult> EditQuestion(int id)
        {
            var question = await _context.Questions
                .Include(q => q.Answers)
                .FirstOrDefaultAsync(q => q.QuestionId == id);

            if (question == null)
                return NotFound();

            return View(question);
        }

        [HttpPost]
        public async Task<IActionResult> EditQuestion(
            int QuestionId,
            string Text,
            List<string> answers,
            int correctAnswerIndex)
        {
            var question = await _context.Questions
                .Include(q => q.Answers)
                .FirstOrDefaultAsync(q => q.QuestionId == QuestionId);

            if (question == null)
                return NotFound();

            question.Text = Text;

            for (int i = 0; i < question.Answers.Count; i++)
            {
                question.Answers.ElementAt(i).Text = answers[i];
                question.Answers.ElementAt(i).IsCorrect = i == correctAnswerIndex;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Questions", new { id = question.QuizId });
        }

        public IActionResult Statistics()
        {
            return View();
        }

    }
}
