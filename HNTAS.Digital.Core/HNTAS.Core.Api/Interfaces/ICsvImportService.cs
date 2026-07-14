using HNTAS.Core.Api.Models;

namespace HNTAS.Core.Api.Interfaces
{
    public interface ICsvImportService
    {
        Task<ImportResult> ImportFromCsvAsync(string fileContent, CancellationToken ct = default);
    }
}
