using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using online_quiz.Data;

namespace online_quiz.Controllers
{
    [Authorize(Roles = "ADMIN")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        // GET: Admin/Results
        public async Task<IActionResult> Results()
        {
            var results = await _context.Results
                .Include(r => r.QuizAttempt)
                    .ThenInclude(a => a.Quiz)
                        .ThenInclude(q => q.Subject)
                .Include(r => r.QuizAttempt)
                    .ThenInclude(a => a.Participant)
                .OrderByDescending(r => r.QuizAttempt.EndTime)
                .ToListAsync();

            return View(results);
        }

        // GET: Admin/ResultDetails
        public async Task<IActionResult> ResultDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _context.Results
                .Include(r => r.QuizAttempt)
                    .ThenInclude(a => a.Quiz)
                        .ThenInclude(q => q.Subject)
                .Include(r => r.QuizAttempt)
                    .ThenInclude(a => a.Participant)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (result == null)
            {
                return NotFound();
            }

            var answers = await _context.StudentAnswers
                .Include(a => a.Question)
                    .ThenInclude(q => q.Options)
                .Include(a => a.SelectedOption)
                .Where(a => a.QuizAttemptId == result.QuizAttemptId)
                .OrderBy(a => a.QuestionId)
                .ToListAsync();

            ViewBag.Answers = answers;

            return View(result);
        }
    }
}