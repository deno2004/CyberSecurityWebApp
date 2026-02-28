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
            var quizzes = await _context.Quizzes
                .ToListAsync();

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
            // ✅ Počisti session, če je prvo vprašanje
            if (questionIndex == 1)
            {
                HttpContext.Session.Remove("UserAnswers");
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
            // 🔹 1. Preberi obstoječe odgovore iz Session
            var existingAnswers = HttpContext.Session.GetString("UserAnswers");

            List<int> answerIds;

            if (string.IsNullOrEmpty(existingAnswers))
                answerIds = new List<int>();
            else
                answerIds = existingAnswers.Split(',').Select(int.Parse).ToList();

            // 🔹 2. Shrani trenutni odgovor
            answerIds.Add(answerId);

            // 🔹 3. Shrani nazaj v Session
            HttpContext.Session.SetString("UserAnswers",
                string.Join(",", answerIds));

            // 🔹 4. Preveri koliko je vprašanj
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

            var userAnswers = HttpContext.Session
                .GetString("UserAnswers");

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

            ViewBag.Score = score;
            ViewBag.Total = questions.Count;
            ViewBag.Results = results;

            return View(quiz);
        }

        /*public async Task<IActionResult> Index()
        {
            var modules = await _context.QuizModules
                .Include(m => m.Questions)
                .ToListAsync();
            return View(modules);
        }

        public async Task<IActionResult> Start(int id)
        {
            var module = await _context.QuizModules
                .Include(m => m.Questions)
                    .ThenInclude(q => q.Answers)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (module == null)
                return NotFound();

            return View(module);
        }

        [HttpPost]
        public async Task<IActionResult> CreateSession(int moduleId, string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                TempData["Error"] = "Prosimo, vnesite vaše ime.";
                return RedirectToAction("Start", new { id = moduleId });
            }

            var sessionCode = GenerateSessionCode();

            var module = await _context.QuizModules
                .Include(m => m.Questions)
                .FirstOrDefaultAsync(m => m.Id == moduleId);

            if (module == null)
                return NotFound();

            var session = new QuizSession
            {
                SessionCode = sessionCode,
                UserName = userName,
                QuizModuleId = moduleId,
                CreatedAt = DateTime.Now,
                TotalQuestions = module.Questions.Count
            };

            _context.QuizSessions.Add(session);
            await _context.SaveChangesAsync();

            return RedirectToAction("TakeQuiz", new { code = sessionCode });
        }

        public async Task<IActionResult> TakeQuiz(string code)
        {
            var session = await _context.QuizSessions
                .Include(s => s.UserAnswers)
                .FirstOrDefaultAsync(s => s.SessionCode == code);

            if (session == null)
                return NotFound();

            if (session.CompletedAt != null)
                return RedirectToAction("Results", new { code = code });

            var module = await _context.QuizModules
                .Include(m => m.Questions)
                    .ThenInclude(q => q.Answers)
                .FirstOrDefaultAsync(m => m.Id == session.QuizModuleId);

            if (module == null)
                return NotFound();

            ViewBag.Session = session;
            ViewBag.CurrentQuestion = session.UserAnswers.Count + 1;

            return View(module);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitAnswer(string code, int questionId, int answerId)
        {
            var session = await _context.QuizSessions
                .Include(s => s.UserAnswers)
                .FirstOrDefaultAsync(s => s.SessionCode == code);

            if (session == null)
                return NotFound();

            var answer = await _context.Answers
                .FirstOrDefaultAsync(a => a.Id == answerId);

            if (answer == null)
                return BadRequest();

            var userAnswer = new UserAnswer
            {
                QuizSessionId = session.Id,
                QuestionId = questionId,
                SelectedAnswerId = answerId,
                IsCorrect = answer.IsCorrect
            };

            _context.UserAnswers.Add(userAnswer);

            if (answer.IsCorrect)
                session.Score++;

            // Check if quiz is completed
            var totalQuestions = await _context.Questions
                .Where(q => q.QuizModuleId == session.QuizModuleId)
                .CountAsync();

            if (session.UserAnswers.Count + 1 >= totalQuestions)
            {
                session.CompletedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            if (session.CompletedAt != null)
                return RedirectToAction("Results", new { code = code });

            return RedirectToAction("TakeQuiz", new { code = code });
        }

        public async Task<IActionResult> Results(string code)
        {
            var session = await _context.QuizSessions
                .Include(s => s.UserAnswers)
                .FirstOrDefaultAsync(s => s.SessionCode == code);

            if (session == null)
                return NotFound();

            var questions = await _context.Questions
                .Include(q => q.Answers)
                .Where(q => q.QuizModuleId == session.QuizModuleId)
                .ToListAsync();

            var results = new List<QuestionResult>();

            foreach (var question in questions)
            {
                var userAnswer = session.UserAnswers
                    .FirstOrDefault(ua => ua.QuestionId == question.Id);

                if (userAnswer != null)
                {
                    var selectedAnswer = question.Answers
                        .First(a => a.Id == userAnswer.SelectedAnswerId);
                    var correctAnswer = question.Answers
                        .First(a => a.IsCorrect);

                    results.Add(new QuestionResult
                    {
                        Question = question,
                        SelectedAnswer = selectedAnswer,
                        CorrectAnswer = correctAnswer,
                        IsCorrect = userAnswer.IsCorrect
                    });
                }
            }

            var percentage = (double)session.Score / session.TotalQuestions * 100;

            var viewModel = new QuizResultViewModel
            {
                Session = session,
                Results = results,
                FeedbackMessage = GetFeedbackMessage(percentage),
                FeedbackClass = GetFeedbackClass(percentage)
            };

            return View(viewModel);
        }

        public IActionResult Share(int id)
        {
            ViewBag.ModuleId = id;
            return View();
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

            var module = await _context.QuizModules
                .Include(m => m.Questions)
                .FirstOrDefaultAsync(m => m.Id == moduleId);

            if (module == null)
                return NotFound();

            var session = new QuizSession
            {
                SessionCode = sessionCode,
                UserName = userName,
                QuizModuleId = moduleId,
                CreatedAt = DateTime.Now,
                TotalQuestions = module.Questions.Count
            };

            _context.QuizSessions.Add(session);
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
        }*/
    }
}
