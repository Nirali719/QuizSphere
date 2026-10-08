using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using online_quiz.Data;
using online_quiz.Models;

namespace online_quiz.Controllers
{
    [Authorize(Roles = "FACULTY")]
    public class QuizController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public QuizController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var faculty = await _userManager.GetUserAsync(User);

            if (faculty == null)
            {
                return Unauthorized();
            }

            var quizzes = await _context.Quizzes
                .Include(q => q.Subject)
                    .ThenInclude(s => s.Category)
                .Where(q => q.FacultyId == faculty.Id)
                .OrderBy(q => q.Title)
                .ToListAsync();

            return View(quizzes);
        }

        // GET: Quiz/Create
        public async Task<IActionResult> Create()
        {
            var subjects = await _context.Subjects
                .Include(s => s.Category)
                .Where(s => s.IsActive)
                .OrderBy(s => s.Category.Name)
                .ThenBy(s => s.Name)
                .ToListAsync();

            ViewBag.Subjects = subjects;

            return View();
        }

        // POST: Quiz/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Quiz quiz)
        {
            var faculty = await _userManager.GetUserAsync(User);

            if (faculty == null)
            {
                return Unauthorized();
            }

            // FacultyId is assigned by the system, not entered in the form.
            ModelState.Remove(nameof(Quiz.FacultyId));

            var subjects = await _context.Subjects
                .Include(s => s.Category)
                .Where(s => s.IsActive)
                .OrderBy(s => s.Category.Name)
                .ThenBy(s => s.Name)
                .ToListAsync();

            if (!ModelState.IsValid)
            {
                ViewBag.Subjects = subjects;

                return View(quiz);
            }

            var subjectExists = await _context.Subjects
                .AnyAsync(s => s.Id == quiz.SubjectId && s.IsActive);

            if (!subjectExists)
            {
                ModelState.AddModelError(
                    "SubjectId",
                    "Please select an active subject.");

                ViewBag.Subjects = subjects;

                return View(quiz);
            }

            if (quiz.PassingMarks > quiz.TotalMarks)
            {
                ModelState.AddModelError(
                    "PassingMarks",
                    "Passing marks cannot be greater than total marks.");

                ViewBag.Subjects = subjects;

                return View(quiz);
            }

            quiz.FacultyId = faculty.Id;
            quiz.Status = "DRAFT";

            _context.Quizzes.Add(quiz);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Quiz/Edit
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

            var quiz = await _context.Quizzes
                .Include(q => q.Subject)
                .FirstOrDefaultAsync(q =>
                    q.Id == id &&
                    q.FacultyId == faculty.Id);

            if (quiz == null)
            {
                return NotFound();
            }

            // A pending quiz cannot be edited while Admin is reviewing it
            if (quiz.Status == "PENDING")
            {
                TempData["Error"] =
                    "This quiz is currently pending Admin approval and cannot be edited.";

                return RedirectToAction(nameof(Index));
            }

            // An approved quiz cannot be edited if participants have already attempted it
            if (quiz.Status == "APPROVED")
            {
                var hasAttempts = await _context.QuizAttempts
                    .AnyAsync(a => a.QuizId == quiz.Id);

                if (hasAttempts)
                {
                    TempData["Error"] =
                        "This approved quiz cannot be edited because participants have already attempted it.";

                    return RedirectToAction(nameof(Index));
                }
            }

            var subjects = await _context.Subjects
                .Include(s => s.Category)
                .Where(s => s.IsActive)
                .OrderBy(s => s.Category.Name)
                .ThenBy(s => s.Name)
                .ToListAsync();

            ViewBag.Subjects = subjects;

            return View(quiz);
        }

        // POST: Quiz/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,Title,Description,SubjectId,TotalMarks,PassingMarks,TimeLimit,MaxAttempts")] Quiz quiz)
        {
            var faculty = await _userManager.GetUserAsync(User);

            if (faculty == null)
            {
                return Unauthorized();
            }

            var existingQuiz = await _context.Quizzes
                .FirstOrDefaultAsync(q =>
                    q.Id == id &&
                    q.FacultyId == faculty.Id);

            if (existingQuiz == null)
            {
                return NotFound();
            }

            // Pending quizzes cannot be edited
            if (existingQuiz.Status == "PENDING")
            {
                TempData["Error"] =
                    "This quiz is currently pending Admin approval and cannot be edited.";

                return RedirectToAction(nameof(Index));
            }

            // Approved quizzes cannot be edited after participants have attempted them
            if (existingQuiz.Status == "APPROVED")
            {
                var hasAttempts = await _context.QuizAttempts
                    .AnyAsync(a => a.QuizId == existingQuiz.Id);

                if (hasAttempts)
                {
                    TempData["Error"] =
                        "This approved quiz cannot be edited because participants have already attempted it.";

                    return RedirectToAction(nameof(Index));
                }
            }

            // Load active subjects with their categories
            var subjects = await _context.Subjects
                .Include(s => s.Category)
                .Where(s => s.IsActive)
                .OrderBy(s => s.Category.Name)
                .ThenBy(s => s.Name)
                .ToListAsync();

            ViewBag.Subjects = subjects;

            ModelState.Remove(nameof(Quiz.Subject));
            ModelState.Remove(nameof(Quiz.Faculty));
            ModelState.Remove(nameof(Quiz.FacultyId));

            if (!ModelState.IsValid)
            {
                return View(quiz);
            }

            var subjectExists = await _context.Subjects
                .AnyAsync(s =>
                    s.Id == quiz.SubjectId &&
                    s.IsActive);

            if (!subjectExists)
            {
                ModelState.AddModelError(
                    "SubjectId",
                    "Please select an active subject.");

                return View(quiz);
            }

            if (quiz.TotalMarks <= 0)
            {
                ModelState.AddModelError(
                    "TotalMarks",
                    "Total marks must be greater than 0.");

                return View(quiz);
            }

            if (quiz.PassingMarks <= 0 ||
                quiz.PassingMarks > quiz.TotalMarks)
            {
                ModelState.AddModelError(
                    "PassingMarks",
                    "Passing marks must be greater than 0 and cannot be greater than total marks.");

                return View(quiz);
            }

            if (quiz.TimeLimit <= 0)
            {
                ModelState.AddModelError(
                    "TimeLimit",
                    "Time limit must be greater than 0.");

                return View(quiz);
            }

            if (quiz.MaxAttempts < 0)
            {
                ModelState.AddModelError(
                    "MaxAttempts",
                    "Maximum attempts cannot be negative.");

                return View(quiz);
            }

            existingQuiz.Title = quiz.Title;
            existingQuiz.Description = quiz.Description;
            existingQuiz.SubjectId = quiz.SubjectId;
            existingQuiz.TotalMarks = quiz.TotalMarks;
            existingQuiz.PassingMarks = quiz.PassingMarks;
            existingQuiz.TimeLimit = quiz.TimeLimit;
            existingQuiz.MaxAttempts = quiz.MaxAttempts;

            // If an approved quiz is edited, it must be approved again
            if (existingQuiz.Status == "APPROVED")
            {
                existingQuiz.Status = "DRAFT";
            }

            // A rejected quiz also returns to Draft after editing
            if (existingQuiz.Status == "REJECTED")
            {
                existingQuiz.Status = "DRAFT";
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Quiz/Delete
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

            var quiz = await _context.Quizzes
                .Include(q => q.Subject)
                .FirstOrDefaultAsync(q =>
                    q.Id == id &&
                    q.FacultyId == faculty.Id);

            if (quiz == null)
            {
                return NotFound();
            }

            // Pending quizzes cannot be deleted
            if (quiz.Status == "PENDING")
            {
                TempData["Error"] =
                    "This quiz is currently pending Admin approval and cannot be deleted.";

                return RedirectToAction(nameof(Index));
            }

            // Approved quizzes cannot be deleted after participants have attempted them
            if (quiz.Status == "APPROVED")
            {
                var hasAttempts = await _context.QuizAttempts
                    .AnyAsync(a => a.QuizId == quiz.Id);

                if (hasAttempts)
                {
                    TempData["Error"] =
                        "This approved quiz cannot be deleted because participants have already attempted it.";

                    return RedirectToAction(nameof(Index));
                }
            }

            return View(quiz);
        }

        // POST: Quiz/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var faculty = await _userManager.GetUserAsync(User);

            if (faculty == null)
            {
                return Unauthorized();
            }

            var quiz = await _context.Quizzes
                .FirstOrDefaultAsync(q =>
                    q.Id == id &&
                    q.FacultyId == faculty.Id);

            if (quiz == null)
            {
                return NotFound();
            }

            // Pending quizzes cannot be deleted
            if (quiz.Status == "PENDING")
            {
                TempData["Error"] =
                    "This quiz is currently pending Admin approval and cannot be deleted.";

                return RedirectToAction(nameof(Index));
            }

            // Approved quizzes cannot be deleted after participants have attempted them
            if (quiz.Status == "APPROVED")
            {
                var hasAttempts = await _context.QuizAttempts
                    .AnyAsync(a => a.QuizId == quiz.Id);

                if (hasAttempts)
                {
                    TempData["Error"] =
                        "This approved quiz cannot be deleted because participants have already attempted it.";

                    return RedirectToAction(nameof(Index));
                }
            }

            var questions = await _context.Questions
                .Where(q => q.QuizId == quiz.Id)
                .ToListAsync();

            foreach (var question in questions)
            {
                var options = await _context.Options
                    .Where(o => o.QuestionId == question.Id)
                    .ToListAsync();

                if (options.Any())
                {
                    _context.Options.RemoveRange(options);
                }
            }

            if (questions.Any())
            {
                _context.Questions.RemoveRange(questions);
            }

            _context.Quizzes.Remove(quiz);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Quiz deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        // POST: Quiz/SubmitForApproval
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitForApproval(int? id)
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

            var quiz = await _context.Quizzes
                .FirstOrDefaultAsync(q =>
                    q.Id == id &&
                    q.FacultyId == faculty.Id);

            if (quiz == null)
            {
                return NotFound();
            }

            if (quiz.Status == "PENDING")
            {
                TempData["Error"] =
                    "This quiz has already been submitted for Admin approval.";

                return RedirectToAction(nameof(Index));
            }

            if (quiz.Status == "APPROVED")
            {
                TempData["Error"] =
                    "This quiz has already been approved by Admin.";

                return RedirectToAction(nameof(Index));
            }

            var questions = await _context.Questions
                .Include(q => q.Options)
                .Where(q => q.QuizId == quiz.Id)
                .ToListAsync();

            if (!questions.Any())
            {
                TempData["Error"] =
                    "You must add at least one question before submitting the quiz for approval.";

                return RedirectToAction(nameof(Index));
            }

            foreach (var question in questions)
            {
                if (question.Options == null || question.Options.Count != 4)
                {
                    TempData["Error"] =
                        $"Question '{question.QuestionText}' must have exactly 4 options.";

                    return RedirectToAction(nameof(Index));
                }

                var correctCount = question.Options.Count(o => o.IsCorrect);

                if (correctCount != 1)
                {
                    TempData["Error"] =
                        $"Question '{question.QuestionText}' must have exactly one correct answer.";

                    return RedirectToAction(nameof(Index));
                }

                if (question.Marks <= 0)
                {
                    TempData["Error"] =
                        $"Question '{question.QuestionText}' must have marks greater than 0.";

                    return RedirectToAction(nameof(Index));
                }
            }

            var totalQuestionMarks = questions.Sum(q => q.Marks);

            if (totalQuestionMarks != quiz.TotalMarks)
            {
                TempData["Error"] =
                    $"Question marks total {totalQuestionMarks}, but quiz total marks are {quiz.TotalMarks}. Please adjust the question marks.";

                return RedirectToAction(nameof(Index));
            }

            quiz.Status = "PENDING";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Quiz has been submitted for Admin approval.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Quiz/Responses
        public async Task<IActionResult> Responses(int? id)
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

            var quiz = await _context.Quizzes
                .Include(q => q.Subject)
                .FirstOrDefaultAsync(q =>
                    q.Id == id &&
                    q.FacultyId == faculty.Id);

            if (quiz == null)
            {
                return NotFound();
            }

            var results = await _context.Results
                .Include(r => r.QuizAttempt)
                    .ThenInclude(a => a.Participant)
                .Where(r =>
                    r.QuizAttempt.QuizId == quiz.Id)
                .OrderByDescending(r => r.QuizAttempt.EndTime)
                .ToListAsync();

            ViewBag.Quiz = quiz;

            return View(results);
        }

        // GET: Quiz/ResponseDetails
        public async Task<IActionResult> ResponseDetails(int? id)
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

            var result = await _context.Results
                .Include(r => r.QuizAttempt)
                    .ThenInclude(a => a.Quiz)
                        .ThenInclude(q => q.Subject)
                .Include(r => r.QuizAttempt)
                    .ThenInclude(a => a.Participant)
                .FirstOrDefaultAsync(r =>
                    r.Id == id &&
                    r.QuizAttempt.Quiz.FacultyId == faculty.Id);

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