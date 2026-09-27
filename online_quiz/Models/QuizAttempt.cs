namespace online_quiz.Models
{
    public class QuizAttempt
    {
        public int Id { get; set; }

        // Participant relationship
        public string ParticipantId { get; set; }
        public ApplicationUser Participant { get; set; }

        // Quiz relationship
        public int QuizId { get; set; }
        public Quiz Quiz { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public bool IsSubmitted { get; set; }
    }
}