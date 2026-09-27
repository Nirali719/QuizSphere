using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using online_quiz.Data;
using online_quiz.Models;

namespace online_quiz.Controllers
{
    [Authorize(Roles = "FACULTY")]
    public class QuestionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public QuestionController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Question/Index
        public async Task<IActionResult> Index(int quizId)
        {
            var faculty = await _userManager.GetUserAsync(User);

            if (faculty == null)
            {
                return Unauthorized();
            }

            var quiz = await _context.Quizzes
                .FirstOrDefaultAsync(q =>
                    q.Id == quizId &&
                    q.FacultyId == faculty.Id);

            if (quiz == null)
            {
                return NotFound();
            }

            var questions = await _context.Questions
                .Where(q => q.QuizId == quizId)
                .Include(q => q.Quiz)
                .OrderBy(q => q.Id)
                .ToListAsync();

            ViewBag.Quiz = quiz;

            return View(questions);
        }

        // GET: Question/Create
        public async Task<IActionResult> Create(int quizId)
        {
            var faculty = await _userManager.GetUserAsync(User);

            if (faculty == null)
            {
                return Unauthorized();
            }

            var quiz = await _context.Quizzes
                .FirstOrDefaultAsync(q =>
                    q.Id == quizId &&
                    q.FacultyId == faculty.Id);

            if (quiz == null)
            {
                return NotFound();
            }
            if (quiz.Status == "PENDING")
            {
                TempData["Error"] =
                    "Questions cannot be added while the quiz is pending Admin approval.";

                return RedirectToAction(
                    nameof(Index),
                    new { quizId = quiz.Id });
            }

            if (quiz.Status == "APPROVED")
            {
                TempData["Error"] =
                    "Questions cannot be added to an approved quiz.";

                return RedirectToAction(
                    nameof(Index),
                    new { quizId = quiz.Id });
            }
            ViewBag.Quiz = quiz;

            return View();
        }

        // POST: Question/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Question question)
        {
            var faculty = await _userManager.GetUserAsync(User);

            if (faculty == null)
            {
                return Unauthorized();
            }

            var quiz = await _context.Quizzes
                .FirstOrDefaultAsync(q =>
                    q.Id == question.QuizId &&
                    q.FacultyId == faculty.Id);

            if (quiz == null)
            {
                return NotFound();
            }
            if (quiz.Status == "PENDING")
            {
                TempData["Error"] =
                    "Questions cannot be added while the quiz is pending Admin approval.";

                return RedirectToAction(
                    nameof(Index),
                    new { quizId = quiz.Id });
            }

            if (quiz.Status == "APPROVED")
            {
                TempData["Error"] =
                    "Questions cannot be added to an approved quiz.";

                return RedirectToAction(
                    nameof(Index),
                    new { quizId = quiz.Id });
            }
            ModelState.Remove(nameof(Question.Quiz));

            if (!ModelState.IsValid)
            {
                ViewBag.Quiz = quiz;

                return View(question);
            }

            question.QuizId = quiz.Id;

            _context.Questions.Add(question);

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Index),
                new { quizId = quiz.Id });
        }

        // GET: Question/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var faculty = await _userManager.GetUserAsync(User);

            if (faculty == null)
            {
                return Unauthorized();
            }

            var question = await _context.Questions
                .Include(q => q.Quiz)
                .FirstOrDefaultAsync(q =>
                    q.Id == id &&
                    q.Quiz.FacultyId == faculty.Id);

            if (question == null)
            {
                return NotFound();
            }
            if (question.Quiz.Status == "PENDING")
            {
                TempData["Error"] =
                    "Questions cannot be edited while the quiz is pending Admin approval.";

                return RedirectToAction(
                    nameof(Index),
                    new { quizId = question.QuizId });
            }

            if (question.Quiz.Status == "APPROVED")
            {
                TempData["Error"] =
                    "Questions cannot be edited in an approved quiz.";

                return RedirectToAction(
                    nameof(Index),
                    new { quizId = question.QuizId });
            }
            return View(question);
        }
        // POST: Question/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Question question)
        {
            if (id != question.Id)
            {
                return NotFound();
            }

            var faculty = await _userManager.GetUserAsync(User);

            if (faculty == null)
            {
                return Unauthorized();
            }

            var existingQuestion = await _context.Questions
                .Include(q => q.Quiz)
                .FirstOrDefaultAsync(q =>
                    q.Id == id &&
                    q.Quiz.FacultyId == faculty.Id);

            if (existingQuestion == null)
            {
                return NotFound();
            }

            if (existingQuestion.Quiz.Status == "PENDING")
            {
                TempData["Error"] =
                    "Questions cannot be edited while the quiz is pending Admin approval.";

                return RedirectToAction(
                    nameof(Index),
                    new { quizId = existingQuestion.QuizId });
            }

            if (existingQuestion.Quiz.Status == "APPROVED")
            {
                TempData["Error"] =
                    "Questions cannot be edited in an approved quiz.";

                return RedirectToAction(
                    nameof(Index),
                    new { quizId = existingQuestion.QuizId });
            }

            // Quiz is a navigation property.
            // It is not submitted by the Edit form.
            ModelState.Remove(nameof(Question.Quiz));

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Where(e => !string.IsNullOrEmpty(e))
                    .ToList();

                ViewBag.Errors = errors;

                question.QuizId = existingQuestion.QuizId;

                return View(question);
            }

            // Update only editable fields
            existingQuestion.QuestionText = question.QuestionText;
            existingQuestion.Marks = question.Marks;

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Index),
                new { quizId = existingQuestion.QuizId });
        }
        // GET: Question/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var faculty = await _userManager.GetUserAsync(User);

            if (faculty == null)
            {
                return Unauthorized();
            }

            var question = await _context.Questions
                .Include(q => q.Quiz)
                .FirstOrDefaultAsync(q =>
                    q.Id == id &&
                    q.Quiz.FacultyId == faculty.Id);

            if (question == null)
            {
                return NotFound();
            }

            return View(question);
        }
        // POST: Question/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var faculty = await _userManager.GetUserAsync(User);

            if (faculty == null)
            {
                return Unauthorized();
            }

            var question = await _context.Questions
                .Include(q => q.Quiz)
                .FirstOrDefaultAsync(q =>
                    q.Id == id &&
                    q.Quiz.FacultyId == faculty.Id);

            if (question == null)
            {
                return NotFound();
            }
            if (question.Quiz.Status == "PENDING")
            {
                TempData["Error"] =
                    "Questions cannot be deleted while the quiz is pending Admin approval.";

                return RedirectToAction(
                    nameof(Index),
                    new { quizId = question.QuizId });
            }

            if (question.Quiz.Status == "APPROVED")
            {
                TempData["Error"] =
                    "Questions cannot be deleted from an approved quiz.";

                return RedirectToAction(
                    nameof(Index),
                    new { quizId = question.QuizId });
            }

            var options = await _context.Options
                .Where(o => o.QuestionId == question.Id)
                .ToListAsync();

            if (options.Any())
            {
                _context.Options.RemoveRange(options);
            }

            _context.Questions.Remove(question);

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Index),
                new { quizId = question.QuizId });
        }
    }
}