using System.ComponentModel.DataAnnotations;

namespace online_quiz.Models
{
    public class Option
    {
        public int Id { get; set; }

        [Required]
        public string OptionText { get; set; }

        public bool IsCorrect { get; set; }

        // Question relationship
        public int QuestionId { get; set; }
        public Question Question { get; set; }
    }
}