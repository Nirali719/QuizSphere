using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using online_quiz.Data;

namespace online_quiz.Controllers
{
    [Authorize(Roles = "ADMIN")]
    public class AdminUserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminUserController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: AdminUser
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users
                .Where(u => u.Email != "admin@quiz.com")
                .ToListAsync();

            var userRoles = new Dictionary<string, string>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                if (roles.Any())
                {
                    userRoles[user.Id] = roles.First();
                }
            }

            ViewBag.UserRoles = userRoles;

            return View(users);
        }

        // POST: AdminUser/Deactivate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            if (user.Email == "admin@quiz.com")
            {
                return BadRequest("The Admin account cannot be deactivated.");
            }

            user.IsActive = false;

            await _userManager.UpdateAsync(user);

            return RedirectToAction(nameof(Index));
        }

        // POST: AdminUser/Activate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            if (user.Email == "admin@quiz.com")
            {
                return BadRequest("The Admin account cannot be modified.");
            }

            user.IsActive = true;

            await _userManager.UpdateAsync(user);

            return RedirectToAction(nameof(Index));
        }
    }
}