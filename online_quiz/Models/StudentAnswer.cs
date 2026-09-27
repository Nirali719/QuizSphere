namespace online_quiz.Models
{
    public class StudentAnswer
    {
        public int Id { get; set; }

        // QuizAttempt relationship
        public int QuizAttemptId { get; set; }
        public QuizAttempt QuizAttempt { get; set; }

        // Question relationship
        public int QuestionId { get; set; }
        public Question Question { get; set; }

        // Selected option
        public int? SelectedOptionId { get; set; }
        public Option SelectedOption { get; set; }
    }
}