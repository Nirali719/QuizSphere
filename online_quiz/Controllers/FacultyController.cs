using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace online_quiz.Controllers
{
    [Authorize(Roles = "FACULTY")]
    public class FacultyController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}