using Microsoft.AspNetCore.Identity;
namespace online_quiz.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        public bool IsActive { get; set; }
    }
}