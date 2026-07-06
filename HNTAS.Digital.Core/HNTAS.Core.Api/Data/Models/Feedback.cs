namespace HNTAS.Core.Api.Data.Models
{
    public class Feedback
    {
        public string? SatisfactionLevel { get; set; }
        public string? FeedbackText { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
