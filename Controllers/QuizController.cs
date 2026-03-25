using CyberSecurityWebApp.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CyberSecurityWebApp.Controllers
{
    public class QuizController : Controller
    {
        private readonly AppDbContext _context;

        public QuizController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var quizzes = await _context.Quizzes.ToListAsync();
            return View(quizzes);
        }

        public async Task<IActionResult> Start(int id)
        {
            var module = await _context.Quizzes
                .FirstOrDefaultAsync(m => m.QuizId == id);

            if (module == null)
                return NotFound();

            return View(module);
        }

        public IActionResult Share(int id)
        {
            ViewBag.ModuleId = id;
            return View();
        }

        public async Task<IActionResult> TakeQuiz(int id, int questionIndex = 1)
        {
            if (questionIndex == 1)
            {
                HttpContext.Session.Remove("UserAnswers");
                HttpContext.Session.Remove("UserName");
            }

            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                    .ThenInclude(q => q.Answers)
                .FirstOrDefaultAsync(q => q.QuizId == id);

            if (quiz == null)
                return NotFound();

            ViewBag.CurrentQuestion = questionIndex;

            return View(quiz);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitAnswer(int quizId, int questionId, int answerId, int questionIndex)
        {
            var existingAnswers = HttpContext.Session.GetString("UserAnswers");

            List<int> answerIds;

            if (string.IsNullOrEmpty(existingAnswers))
                answerIds = new List<int>();
            else
                answerIds = existingAnswers.Split(',').Select(int.Parse).ToList();

            answerIds.Add(answerId);

            HttpContext.Session.SetString("UserAnswers", string.Join(",", answerIds));

            var totalQuestions = await _context.Questions
                .CountAsync(q => q.QuizId == quizId);

            if (questionIndex < totalQuestions)
            {
                return RedirectToAction("TakeQuiz", new
                {
                    id = quizId,
                    questionIndex = questionIndex + 1
                });
            }

            return RedirectToAction("Result", new { id = quizId });
        }

        public async Task<IActionResult> Result(int id)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                    .ThenInclude(q => q.Answers)
                .FirstOrDefaultAsync(q => q.QuizId == id);

            if (quiz == null)
                return NotFound();

            var userAnswers = HttpContext.Session.GetString("UserAnswers");

            var answerIds = string.IsNullOrEmpty(userAnswers)
                ? new List<int>()
                : userAnswers.Split(',').Select(int.Parse).ToList();

            int score = 0;
            var results = new List<dynamic>();

            var questions = quiz.Questions
                .OrderBy(q => q.QuestionId)
                .ToList();

            for (int i = 0; i < questions.Count; i++)
            {
                var question = questions[i];
                var selectedAnswerId = answerIds.ElementAtOrDefault(i);

                var selectedAnswer = question.Answers
                    .FirstOrDefault(a => a.AnswerId == selectedAnswerId);

                var correctAnswer = question.Answers
                    .FirstOrDefault(a => a.IsCorrect);

                bool isCorrect = selectedAnswer != null && selectedAnswer.IsCorrect;

                if (isCorrect) score++;

                results.Add(new
                {
                    Question = question,
                    SelectedAnswer = selectedAnswer,
                    CorrectAnswer = correctAnswer,
                    IsCorrect = isCorrect
                });
            }

            double percentage = questions.Count > 0 ? (double)score / questions.Count * 100 : 0;
            bool passed = percentage >= quiz.PassThreshold;

            ViewBag.Score = score;
            ViewBag.Total = questions.Count;
            ViewBag.Results = results;
            ViewBag.Percentage = percentage;
            ViewBag.Passed = passed;
            ViewBag.FeedbackMessage = GetFeedbackMessage(percentage);
            ViewBag.FeedbackClass = GetFeedbackClass(percentage);

            // Shrani rezultat v session za certifikat
            if (passed)
            {
                HttpContext.Session.SetString($"CertScore_{id}", score.ToString());
                HttpContext.Session.SetString($"CertTotal_{id}", questions.Count.ToString());
                HttpContext.Session.SetString($"CertDate_{id}", DateTime.Now.ToString("dd. MM. yyyy"));
            }

            return View(quiz);
        }

        // ✅ Nova akcija za prikaz certifikata
        public async Task<IActionResult> Certificate(int id, string userName)
        {
            var quiz = await _context.Quizzes
                .FirstOrDefaultAsync(q => q.QuizId == id);

            if (quiz == null)
                return NotFound();

            var scoreStr = HttpContext.Session.GetString($"CertScore_{id}");
            var totalStr = HttpContext.Session.GetString($"CertTotal_{id}");
            var date = HttpContext.Session.GetString($"CertDate_{id}");

            if (string.IsNullOrEmpty(scoreStr))
                return RedirectToAction("Result", new { id });

            int score = int.Parse(scoreStr);
            int total = int.Parse(totalStr!);
            double percentage = total > 0 ? (double)score / total * 100 : 0;

            ViewBag.Score = score;
            ViewBag.Total = total;
            ViewBag.Percentage = percentage;
            ViewBag.CertDate = date ?? DateTime.Now.ToString("dd. MM. yyyy");
            ViewBag.UserName = string.IsNullOrWhiteSpace(userName) ? "Udeleženec" : userName;

            return View("~/Views/Certificate/Certificate.cshtml", quiz);
        }

        [HttpPost]
        public async Task<IActionResult> GenerateLink(int moduleId, string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                TempData["Error"] = "Prosimo, vnesite vaše ime.";
                return RedirectToAction("Share", new { id = moduleId });
            }

            var sessionCode = GenerateSessionCode();

            var module = await _context.Quizzes
                .Include(m => m.Questions)
                .FirstOrDefaultAsync(m => m.QuizId == moduleId);

            if (module == null)
                return NotFound();

            var session = new Models.Quiz
            {
                Title = $"{module.Title} - Seja za {userName}",
                Description = $"Seja kviza za uporabnika {userName}",
                IconClass = module.IconClass,
                PassThreshold = module.PassThreshold
            };

            _context.Quizzes.Add(session);
            await _context.SaveChangesAsync();

            var shareUrl = Url.Action("TakeQuiz", "Quiz", new { code = sessionCode }, Request.Scheme);
            ViewBag.ShareUrl = shareUrl;
            ViewBag.SessionCode = sessionCode;
            ViewBag.ModuleId = moduleId;

            return View("Share");
        }

        private string GenerateSessionCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private string GetFeedbackMessage(double percentage)
        {
            return percentage switch
            {
                >= 90 => "Odlično! Imate izvrstno znanje o kibernetski varnosti!",
                >= 70 => "Zelo dobro! Dobro razumete osnove kibernetske varnosti.",
                >= 50 => "Dobro! Še nekaj vsebin bi bilo dobro pregledati.",
                _ => "Priporočamo, da si ogledate izobraževalne vsebine in poskusite znova."
            };
        }

        private string GetFeedbackClass(double percentage)
        {
            return percentage switch
            {
                >= 90 => "excellent",
                >= 70 => "good",
                >= 50 => "average",
                _ => "needs-improvement"
            };
        }
    }
}