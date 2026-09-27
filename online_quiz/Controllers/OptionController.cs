using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using online_quiz.Data;
using online_quiz.Models;

namespace online_quiz.Controllers
{
    [Authorize(Roles = "FACULTY")]
    public class OptionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OptionController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Option/Index
        public async Task<IActionResult> Index(int questionId)
        {
            var faculty = await _userManager.GetUserAsync(User);

            if (faculty == null)
            {
                return Unauthorized();
            }

            var question = await _context.Questions
                .Include(q => q.Quiz)
                .FirstOrDefaultAsync(q =>
                    q.Id == questionId &&
                    q.Quiz.FacultyId == faculty.Id);

            if (question == null)
            {
                return NotFound();
            }

            var options = await _context.Options
                .Where(o => o.QuestionId == questionId)
                .OrderBy(o => o.Id)
                .ToListAsync();

            ViewBag.Question = question;

            return View(options);
        }

        // GET: Option/Create
        public async Task<IActionResult> Create(int questionId)
        {
            var faculty = await _userManager.GetUserAsync(User);

            if (faculty == null)
            {
                return Unauthorized();
            }

            var question = await _context.Questions
                .Include(q => q.Quiz)
                .FirstOrDefaultAsync(q =>
                    q.Id == questionId &&
                    q.Quiz.FacultyId == faculty.Id);

            if (question == null)
            {
                return NotFound();
            }

            if (question.Quiz.Status == "PENDING")
            {
                TempData["Error"] =
                    "Options cannot be added while the quiz is pending Admin approval.";

                return RedirectToAction(
                    nameof(Index),
                    new { questionId = question.Id });
            }

            if (question.Quiz.Status == "APPROVED")
            {
                TempData["Error"] =
                    "Options cannot be added to an approved quiz.";

                return RedirectToAction(
                    nameof(Index),
                    new { questionId = question.Id });
            }

            ViewBag.Question = question;

            return View();
        }

        // POST: Option/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Option option)
        {
            var faculty = await _userManager.GetUserAsync(User);

            if (faculty == null)
            {
                return Unauthorized();
            }

            var question = await _context.Questions
                .Include(q => q.Quiz)
                .FirstOrDefaultAsync(q =>
                    q.Id == option.QuestionId &&
                    q.Quiz.FacultyId == faculty.Id);

            if (question == null)
            {
                return NotFound();
            }

            if (question.Quiz.Status == "PENDING")
            {
                TempData["Error"] =
                    "Options cannot be added while the quiz is pending Admin approval.";

                return RedirectToAction(
                    nameof(Index),
                    new { questionId = question.Id });
            }

            if (question.Quiz.Status == "APPROVED")
            {
                TempData["Error"] =
                    "Options cannot be added to an approved quiz.";

                return RedirectToAction(
                    nameof(Index),
                    new { questionId = question.Id });
            }

            var optionCount = await _context.Options
    .CountAsync(o => o.QuestionId == question.Id);

            if (optionCount >= 4)
            {
                ModelState.AddModelError(
                    "OptionText",
                    "A question can have maximum 4 options.");

                ViewBag.Question = question;

                return View(option);
            }

            if (option.IsCorrect)
            {
                var correctOptionExists = await _context.Options
                    .AnyAsync(o =>
                        o.QuestionId == question.Id &&
                        o.IsCorrect);

                if (correctOptionExists)
                {
                    ModelState.AddModelError(
                        "IsCorrect",
                        "This question already has a correct answer. Only one correct answer is allowed.");

                    ViewBag.Question = question;

                    return View(option);
                }
            }

            ModelState.Remove(nameof(Option.Question));

            if (!ModelState.IsValid)
            {
                ViewBag.Question = question;

                return View(option);
            }

            option.QuestionId = question.Id;

            _context.Options.Add(option);

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Index),
                new { questionId = question.Id });
        }

        // GET: Option/Edit
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

            var option = await _context.Options
                .Include(o => o.Question)
                .ThenInclude(q => q.Quiz)
                .FirstOrDefaultAsync(o =>
                    o.Id == id &&
                    o.Question.Quiz.FacultyId == faculty.Id);

            if (option == null)
            {
                return NotFound();
            }
            if (option.Question.Quiz.Status == "PENDING")
            {
                TempData["Error"] =
                    "Options cannot be edited while the quiz is pending Admin approval.";

                return RedirectToAction(
                    nameof(Index),
                    new { questionId = option.QuestionId });
            }

            if (option.Question.Quiz.Status == "APPROVED")
            {
                TempData["Error"] =
                    "Options cannot be edited in an approved quiz.";

                return RedirectToAction(
                    nameof(Index),
                    new { questionId = option.QuestionId });
            }
            return View(option);
        }

        // POST: Option/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,OptionText,IsCorrect,QuestionId")] Option option)
        {
            var faculty = await _userManager.GetUserAsync(User);

            if (faculty == null)
            {
                return Unauthorized();
            }

            var existingOption = await _context.Options
                .Include(o => o.Question)
                .ThenInclude(q => q.Quiz)
                .FirstOrDefaultAsync(o =>
                    o.Id == id &&
                    o.Question.Quiz.FacultyId == faculty.Id);

            if (existingOption == null)
            {
                return NotFound();
            }
            if (existingOption.Question.Quiz.Status == "PENDING")
            {
                TempData["Error"] =
                    "Options cannot be edited while the quiz is pending Admin approval.";

                return RedirectToAction(
                    nameof(Index),
                    new { questionId = existingOption.QuestionId });
            }

            if (existingOption.Question.Quiz.Status == "APPROVED")
            {
                TempData["Error"] =
                    "Options cannot be edited in an approved quiz.";

                return RedirectToAction(
                    nameof(Index),
                    new { questionId = existingOption.QuestionId });
            }

            ModelState.Remove(nameof(Option.Question));

            if (!ModelState.IsValid)
            {
                option.Question = existingOption.Question;

                return View(option);
            }

            // Check if another option is already correct
            if (option.IsCorrect)
            {
                var anotherCorrectOption = await _context.Options
                    .AnyAsync(o =>
                        o.QuestionId == existingOption.QuestionId &&
                        o.Id != existingOption.Id &&
                        o.IsCorrect);

                if (anotherCorrectOption)
                {
                    ModelState.AddModelError(
                        "IsCorrect",
                        "This question already has another correct answer. Only one correct answer is allowed.");

                    option.Question = existingOption.Question;

                    return View(option);
                }
            }

            existingOption.OptionText = option.OptionText;
            existingOption.IsCorrect = option.IsCorrect;

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Index),
                new
                {
                    questionId = existingOption.QuestionId
                });
        }

        // GET: Option/Delete
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

            var option = await _context.Options
                .Include(o => o.Question)
                .ThenInclude(q => q.Quiz)
                .FirstOrDefaultAsync(o =>
                    o.Id == id &&
                    o.Question.Quiz.FacultyId == faculty.Id);

            if (option == null)
            {
                return NotFound();
            }
            if (option.Question.Quiz.Status == "PENDING")
            {
                TempData["Error"] =
                    "Options cannot be deleted while the quiz is pending Admin approval.";

                return RedirectToAction(
                    nameof(Index),
                    new { questionId = option.QuestionId });
            }

            if (option.Question.Quiz.Status == "APPROVED")
            {
                TempData["Error"] =
                    "Options cannot be deleted from an approved quiz.";

                return RedirectToAction(
                    nameof(Index),
                    new { questionId = option.QuestionId });
            }
            return View(option);
        }

        // POST: Option/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var faculty = await _userManager.GetUserAsync(User);

            if (faculty == null)
            {
                return Unauthorized();
            }

            var option = await _context.Options
                .Include(o => o.Question)
                .ThenInclude(q => q.Quiz)
                .FirstOrDefaultAsync(o =>
                    o.Id == id &&
                    o.Question.Quiz.FacultyId == faculty.Id);

            if (option == null)
            {
                return NotFound();
            }
            if (option.Question.Quiz.Status == "PENDING")
            {
                TempData["Error"] =
                    "Options cannot be deleted while the quiz is pending Admin approval.";

                return RedirectToAction(
                    nameof(Index),
                    new { questionId = option.QuestionId });
            }

            if (option.Question.Quiz.Status == "APPROVED")
            {
                TempData["Error"] =
                    "Options cannot be deleted from an approved quiz.";

                return RedirectToAction(
                    nameof(Index),
                    new { questionId = option.QuestionId });
            }
            int questionId = option.QuestionId;

            _context.Options.Remove(option);

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Index),
                new { questionId = questionId });
        }



        // GET: Option/Finish
        public async Task<IActionResult> Finish(int questionId)
        {
            var faculty = await _userManager.GetUserAsync(User);

            if (faculty == null)
            {
                return Unauthorized();
            }

            var question = await _context.Questions
                .Include(q => q.Quiz)
                .FirstOrDefaultAsync(q =>
                    q.Id == questionId &&
                    q.Quiz.FacultyId == faculty.Id);

            if (question == null)
            {
                return NotFound();
            }

            var options = await _context.Options
                .Where(o => o.QuestionId == questionId)
                .ToListAsync();

            if (options.Count != 4)
            {
                TempData["Error"] =
                    "A question must have exactly 4 options.";

                return RedirectToAction(
                    nameof(Index),
                    new { questionId = questionId });
            }

            var correctCount = options.Count(o => o.IsCorrect);

            if (correctCount != 1)
            {
                TempData["Error"] =
                    "A question must have exactly one correct answer.";

                return RedirectToAction(
                    nameof(Index),
                    new { questionId = questionId });
            }

            TempData["Success"] =
                "Question is complete and has exactly one correct answer.";

            return RedirectToAction(
                nameof(Index),
                new { questionId = questionId });
        }
    }
}