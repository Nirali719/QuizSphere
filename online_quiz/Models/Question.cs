using System.ComponentModel.DataAnnotations;

namespace online_quiz.Models
{
    public class Question
    {
        public int Id { get; set; }

        [Required]
        public string QuestionText { get; set; }

        public int Marks { get; set; }

        public int QuizId { get; set; }

        public Quiz? Quiz { get; set; }

        public ICollection<Option>? Options { get; set; }
    }
}