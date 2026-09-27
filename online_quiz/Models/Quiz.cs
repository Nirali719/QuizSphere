using System.ComponentModel.DataAnnotations;

namespace online_quiz.Models
{
    public class Quiz
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Title { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public int TotalMarks { get; set; }

        public int PassingMarks { get; set; }

        public int TimeLimit { get; set; }

        public int MaxAttempts { get; set; }

        public string Status { get; set; } = "DRAFT";

        // Subject relationship
        public int SubjectId { get; set; }
        public Subject? Subject { get; set; }

        // Faculty relationship
        public string FacultyId { get; set; }
        public ApplicationUser? Faculty { get; set; }
        public ICollection<Question>? Questions { get; set; }
    }
}