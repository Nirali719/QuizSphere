using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using online_quiz.Data;

namespace online_quiz.Controllers
{
    [Authorize(Roles = "ADMIN")]
    public class AdminQuizController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminQuizController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: AdminQuiz
        public async Task<IActionResult> Index()
        {
            var quizzes = await _context.Quizzes
                .Include(q => q.Subject)
                .Include(q => q.Faculty)
                .OrderBy(q => q.Status)
                .ThenBy(q => q.Title)
                .ToListAsync();

            return View(quizzes);
        }
        // POST: AdminQuiz/Approve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var quiz = await _context.Quizzes
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null)
            {
                return NotFound();
            }

            if (quiz.Status != "PENDING")
            {
                return BadRequest(
                    "Only pending quizzes can be approved.");
            }

            var questions = await _context.Questions
                .Include(q => q.Options)
                .Where(q => q.QuizId == quiz.Id)
                .ToListAsync();

            if (!questions.Any())
            {
                return BadRequest(
                    "This quiz cannot be approved because it has no questions.");
            }

            foreach (var question in questions)
            {
                if (question.Options == null ||
                    question.Options.Count != 4)
                {
                    return BadRequest(
                        $"Question '{question.QuestionText}' must have exactly 4 options.");
                }

                var correctCount = question.Options
                    .Count(o => o.IsCorrect);

                if (correctCount != 1)
                {
                    return BadRequest(
                        $"Question '{question.QuestionText}' must have exactly one correct answer.");
                }

                if (question.Marks <= 0)
                {
                    return BadRequest(
                        $"Question '{question.QuestionText}' must have marks greater than 0.");
                }
            }

            var totalQuestionMarks = questions.Sum(q => q.Marks);

            if (totalQuestionMarks != quiz.TotalMarks)
            {
                return BadRequest(
                    $"Question marks total {totalQuestionMarks}, but quiz total marks are {quiz.TotalMarks}.");
            }

            quiz.Status = "APPROVED";

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        // POST: AdminQuiz/Reject
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var quiz = await _context.Quizzes
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null)
            {
                return NotFound();
            }

            if (quiz.Status != "PENDING")
            {
                return BadRequest("Only pending quizzes can be rejected.");
            }

            quiz.Status = "REJECTED";

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}