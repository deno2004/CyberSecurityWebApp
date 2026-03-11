using CyberSecurityWebApp.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CyberSecurityWebApp.Controllers
{
    public class CourseController : Controller
    {
        private readonly AppDbContext _context;

        public CourseController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var course = _context.Courses
                .Include(c => c.Quiz)
                .ThenInclude(q => q.Questions)
                .ToList();

            return View(course);
        }

    }
}
