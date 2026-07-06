using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Models.Assessor;

namespace HNTAS.Core.Api.Interfaces
{
    public interface IAssessorService
    {
        Task<List<AssessorSearchResult>> GetAssessorSuggestionsAsync(string searchTerm);
        //Task CreateAssessorAsync(Assessor assessor);
    }
}
