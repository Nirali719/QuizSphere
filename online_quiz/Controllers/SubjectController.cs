using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using online_quiz.Data;
using online_quiz.Models;

namespace online_quiz.Controllers
{
    [Authorize(Roles = "ADMIN")]
    public class SubjectController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SubjectController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var subjects = await _context.Subjects
                .Include(s => s.Category)
                .OrderBy(s => s.Category.Name)
                .ThenBy(s => s.Name)
                .ToListAsync();

            return View(subjects);
        }

        // GET: Subject/Create
        public async Task<IActionResult> Create(int categoryId)
        {
            var category = await _context.Categories.FindAsync(categoryId);

            if (category == null || !category.IsActive)
            {
                return NotFound();
            }

            ViewBag.CategoryName = category.Name;

            return View(new Subject
            {
                CategoryId = categoryId
            });
        }

        // POST: Subject/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Subject subject)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == subject.CategoryId);

            if (category == null || !category.IsActive)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                subject.IsActive = true;

                _context.Subjects.Add(subject);

                await _context.SaveChangesAsync();

                return RedirectToAction(
                    "Subjects",
                    "Category",
                    new { id = subject.CategoryId }
                );
            }

            ViewBag.CategoryName = category.Name;

            return View(subject);
        }

        // GET: Subject/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var subject = await _context.Subjects
                .Include(s => s.Category)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subject == null)
            {
                return NotFound();
            }

            if (!subject.IsActive)
            {
                return NotFound();
            }

            return View(subject);
        }

        // POST: Subject/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Subject subject)
        {
            if (id != subject.Id)
            {
                return NotFound();
            }

            var existingSubject = await _context.Subjects
                .Include(s => s.Category)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (existingSubject == null)
            {
                return NotFound();
            }

            if (!existingSubject.IsActive)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                existingSubject.Name = subject.Name;
                existingSubject.Description = subject.Description;

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            subject.Category = existingSubject.Category;

            return View(subject);
        }

        // GET: Subject/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var subject = await _context.Subjects.FindAsync(id);

            if (subject == null)
            {
                return NotFound();
            }

            return View(subject);
        }

        // POST: Subject/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var subject = await _context.Subjects.FindAsync(id);

            if (subject == null)
            {
                return NotFound();
            }

            subject.IsActive = false;

            _context.Update(subject);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Subject/Activate/5
        public async Task<IActionResult> Activate(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var subject = await _context.Subjects
                .Include(s => s.Category)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subject == null)
            {
                return NotFound();
            }

            if (subject.Category == null || !subject.Category.IsActive)
            {
                return NotFound();
            }

            subject.IsActive = true;

            _context.Update(subject);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}