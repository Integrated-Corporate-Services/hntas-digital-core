using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Data.Models
{
    [ExcludeFromCodeCoverage]
    public class Feedback
    {
        public string? SatisfactionLevel { get; set; }
        public string? FeedbackText { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
