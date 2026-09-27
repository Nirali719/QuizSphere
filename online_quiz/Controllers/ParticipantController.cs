using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using online_quiz.Data;
using online_quiz.Models;

namespace online_quiz.Controllers
{
    [Authorize(Roles = "PARTICIPANT")]
    public class ParticipantController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ParticipantController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        // GET: Participant/Subjects
        public async Task<IActionResult> Subjects()
        {
            var subjects = await _context.Subjects
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();

            return View(subjects);
        }

        // GET: Participant/Quizzes
        public async Task<IActionResult> Quizzes(int? subjectId)
        {
            var participant = await _userManager.GetUserAsync(User);

            if (participant == null)
            {
                return Unauthorized();
            }

            var quizQuery = _context.Quizzes
                .Include(q => q.Subject)
                .Include(q => q.Faculty)
                .Where(q => q.Status == "APPROVED");

            if (subjectId.HasValue)
            {
                quizQuery = quizQuery
                    .Where(q => q.SubjectId == subjectId.Value);
            }

            var quizzes = await quizQuery
                .OrderBy(q => q.Subject.Name)
                .ThenBy(q => q.Title)
                .ToListAsync();

            var attemptCounts = await _context.QuizAttempts
                .Where(a => a.ParticipantId == participant.Id)
                .GroupBy(a => a.QuizId)
                .Select(g => new
                {
                    QuizId = g.Key,
                    Count = g.Count()
                })
                .ToDictionaryAsync(
                    x => x.QuizId,
                    x => x.Count);

            ViewBag.AttemptCounts = attemptCounts;

            return View(quizzes);
        }

        // GET: Participant/Attempt
        public async Task<IActionResult> Attempt(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var participant = await _userManager.GetUserAsync(User);

            if (participant == null)
            {
                return Unauthorized();
            }

            var quiz = await _context.Quizzes
                .Include(q => q.Subject)
                .Include(q => q.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(q =>
                    q.Id == id &&
                    q.Status == "APPROVED");

            if (quiz == null)
            {
                return NotFound();
            }

            // Check if participant already has an unfinished attempt
            var existingAttempt = await _context.QuizAttempts
                .FirstOrDefaultAsync(a =>
                    a.QuizId == quiz.Id &&
                    a.ParticipantId == participant.Id &&
                    !a.IsSubmitted);

            // If there is no unfinished attempt, check maximum attempts
            if (existingAttempt == null)
            {
                var attemptCount = await _context.QuizAttempts
                    .CountAsync(a =>
                        a.QuizId == quiz.Id &&
                        a.ParticipantId == participant.Id);

                if (quiz.MaxAttempts > 0 &&
                    attemptCount >= quiz.MaxAttempts)
                {
                    TempData["Error"] =
                        $"You have already used all {quiz.MaxAttempts} attempts for this quiz. You cannot attempt this quiz again.";

                    return RedirectToAction(nameof(Quizzes));
                }

                // Create a new attempt
                existingAttempt = new QuizAttempt
                {
                    ParticipantId = participant.Id,
                    QuizId = quiz.Id,
                    StartTime = DateTime.Now,
                    IsSubmitted = false
                };

                _context.QuizAttempts.Add(existingAttempt);

                await _context.SaveChangesAsync();
            }

            // Calculate remaining time from the server
            var elapsedSeconds =
                (DateTime.Now - existingAttempt.StartTime).TotalSeconds;

            var totalSeconds = quiz.TimeLimit * 60;

            var remainingSeconds =
                Math.Max(0, totalSeconds - (int)elapsedSeconds);

            // If the server says the attempt has already expired,
            // submit it with all questions unattempted.
            if (remainingSeconds <= 0)
            {
                int totalQuestions = quiz.Questions?.Count ?? 0;

                var result = new Result
                {
                    QuizAttemptId = existingAttempt.Id,
                    TotalQuestions = totalQuestions,
                    CorrectAnswers = 0,
                    IncorrectAnswers = 0,
                    Unattempted = totalQuestions,
                    Score = 0,
                    TotalMarks = quiz.TotalMarks,
                    Percentage = 0,
                    IsPassed = false
                };

                _context.Results.Add(result);

                existingAttempt.IsSubmitted = true;
                existingAttempt.EndTime = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["TimeExpired"] =
                    "The quiz time limit was reached. Your quiz has been submitted automatically.";

                return RedirectToAction(
                    nameof(Result),
                    new { id = result.Id });
            }

            // Send attempt ID to the view
            ViewBag.AttemptId = existingAttempt.Id;

            // Send remaining time calculated by the server
            ViewBag.RemainingSeconds = remainingSeconds;

            return View(quiz);
        }

        // POST: Participant/SubmitAttempt
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitAttempt(
            int QuizId,
            int AttemptId,
            Dictionary<int, int?> Answers)
        {
            var participant = await _userManager.GetUserAsync(User);

            if (participant == null)
            {
                return Unauthorized();
            }

            var attempt = await _context.QuizAttempts
                .Include(a => a.Quiz)
                .FirstOrDefaultAsync(a =>
                    a.Id == AttemptId &&
                    a.QuizId == QuizId &&
                    a.ParticipantId == participant.Id);

            if (attempt == null)
            {
                return NotFound();
            }

            if (attempt.IsSubmitted)
            {
                return BadRequest(
                    "This quiz attempt has already been submitted.");
            }

            // Calculate elapsed time on the SERVER
            var elapsedSeconds =
                (DateTime.Now - attempt.StartTime).TotalSeconds;

            var totalSeconds =
                attempt.Quiz.TimeLimit * 60;

            bool timeExpired =
                elapsedSeconds >= totalSeconds;

            // Load quiz and questions
            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(q =>
                    q.Id == QuizId &&
                    q.Status == "APPROVED");

            if (quiz == null)
            {
                return NotFound();
            }

            int correctAnswers = 0;
            int incorrectAnswers = 0;
            int unattempted = 0;
            int score = 0;

            foreach (var question in
                     quiz.Questions ?? new List<Question>())
            {
                int? selectedOptionId = null;

                if (Answers != null &&
                    Answers.ContainsKey(question.Id))
                {
                    selectedOptionId = Answers[question.Id];
                }

                var studentAnswer = new StudentAnswer
                {
                    QuizAttemptId = attempt.Id,
                    QuestionId = question.Id,
                    SelectedOptionId = selectedOptionId
                };

                _context.StudentAnswers.Add(studentAnswer);

                if (selectedOptionId == null)
                {
                    unattempted++;
                }
                else
                {
                    var selectedOption = question.Options?
                        .FirstOrDefault(o =>
                            o.Id == selectedOptionId);

                    if (selectedOption != null &&
                        selectedOption.IsCorrect)
                    {
                        correctAnswers++;
                        score += question.Marks;
                    }
                    else
                    {
                        incorrectAnswers++;
                    }
                }
            }

            int totalQuestions =
                quiz.Questions?.Count ?? 0;

            int totalMarks = quiz.TotalMarks;

            double percentage =
                totalMarks > 0
                    ? (double)score / totalMarks * 100
                    : 0;

            bool isPassed =
                score >= quiz.PassingMarks;

            var result = new Result
            {
                QuizAttemptId = attempt.Id,
                TotalQuestions = totalQuestions,
                CorrectAnswers = correctAnswers,
                IncorrectAnswers = incorrectAnswers,
                Unattempted = unattempted,
                Score = score,
                TotalMarks = totalMarks,
                Percentage = percentage,
                IsPassed = isPassed
            };

            _context.Results.Add(result);

            attempt.IsSubmitted = true;
            attempt.EndTime = DateTime.Now;

            if (timeExpired)
            {
                TempData["TimeExpired"] =
                    "The quiz time limit was reached. Your answers have been submitted automatically.";
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Result),
                new { id = result.Id });
        }

        // GET: Participant/Result
        public async Task<IActionResult> Result(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var participant = await _userManager.GetUserAsync(User);

            if (participant == null)
            {
                return Unauthorized();
            }

            var result = await _context.Results
                .Include(r => r.QuizAttempt)
                    .ThenInclude(a => a.Quiz)
                        .ThenInclude(q => q.Subject)
                .FirstOrDefaultAsync(r =>
                    r.Id == id &&
                    r.QuizAttempt.ParticipantId == participant.Id);

            if (result == null)
            {
                return NotFound();
            }

            var answers = await _context.StudentAnswers
                .Include(a => a.Question)
                    .ThenInclude(q => q.Options)
                .Include(a => a.SelectedOption)
                .Where(a =>
                    a.QuizAttemptId == result.QuizAttemptId)
                .ToListAsync();

            ViewBag.Answers = answers;

            return View(result);
        }

        // GET: Participant/History
        public async Task<IActionResult> History()
        {
            var participant = await _userManager.GetUserAsync(User);

            if (participant == null)
            {
                return Unauthorized();
            }

            var results = await _context.Results
                .Include(r => r.QuizAttempt)
                    .ThenInclude(a => a.Quiz)
                        .ThenInclude(q => q.Subject)
                .Where(r =>
                    r.QuizAttempt.ParticipantId == participant.Id)
                .OrderByDescending(r =>
                    r.QuizAttempt.EndTime)
                .ToListAsync();

            return View(results);
        }

        // GET: Participant/Progress
        public async Task<IActionResult> Progress()
        {
            var participant = await _userManager.GetUserAsync(User);

            if (participant == null)
            {
                return Unauthorized();
            }

            var results = await _context.Results
                .Include(r => r.QuizAttempt)
                    .ThenInclude(a => a.Quiz)
                        .ThenInclude(q => q.Subject)
                .Where(r =>
                    r.QuizAttempt.ParticipantId == participant.Id)
                .ToListAsync();

            var progress = results
                .GroupBy(r => r.QuizAttempt.Quiz.Subject)
                .Select(g => new
                {
                    SubjectName = g.Key.Name,
                    QuizzesAttempted = g.Count(),
                    AverageScore = g.Average(r => r.Score),
                    AveragePercentage =
                        g.Average(r => r.Percentage)
                })
                .OrderBy(p => p.SubjectName)
                .ToList();

            return View(progress);
        }
    }
}