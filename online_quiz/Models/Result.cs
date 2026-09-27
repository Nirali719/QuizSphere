namespace online_quiz.Models
{
    public class Result
    {
        public int Id { get; set; }

        public int QuizAttemptId { get; set; }
        public QuizAttempt QuizAttempt { get; set; }

        public int TotalQuestions { get; set; }

        public int CorrectAnswers { get; set; }

        public int IncorrectAnswers { get; set; }

        public int Unattempted { get; set; }

        public int Score { get; set; }

        public int TotalMarks { get; set; }

        public double Percentage { get; set; }

        public bool IsPassed { get; set; }
    }
}