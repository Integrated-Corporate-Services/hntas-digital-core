using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Models
{
    [ExcludeFromCodeCoverage]
    public class CreateFeedbackRequest
    {
        public string? SatisfactionLevel { get; set; }
        public string? FeedbackText { get; set; }
    }
}
