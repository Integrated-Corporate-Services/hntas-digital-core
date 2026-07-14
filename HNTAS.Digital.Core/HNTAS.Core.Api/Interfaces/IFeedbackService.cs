using HNTAS.Core.Api.Models;

namespace HNTAS.Core.Api.Interfaces
{
    public interface IFeedbackService
    {
        Task CreateAsync(CreateFeedbackRequest request);
    }
}
