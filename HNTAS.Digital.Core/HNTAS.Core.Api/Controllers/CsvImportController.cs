// PSEUDOCODE / PLAN (detailed):
// 1. Create an import result DTO to summarise counts and errors.
// 2. Define interface `ICsvImportService` with a single async method:
//      Task<ImportResult> ImportFromCsvAsync(IFormFile file, CancellationToken ct = default);
// 3. Implement `CsvImportService`:
//    - Inject IMongoDatabase, IOptions<AWSDocDbSettings>, ILogger<CsvImportService>
//    - Get collections as BsonDocument for Organisations, HeatNetworks, Users (names from settings)
//    - Read CSV from the provided IFormFile using a StreamReader
//    - Expect header with columns: hnId, hnName, hnLocation, organisationId, organisationName, userEmailId
//    - Map header indices to column names; tolerate different ordering; skip unknown columns
//    - For each non-empty data row:
//       a. Extract values and trim
//       b. If hnId or organisationId missing -> record error and skip
//       c. Upsert organisation: use Update with SetOnInsert for org fields, AddToSet for hnIds; IsUpsert = true
//       d. Upsert heat network: replace or update document keyed by hnId; SetOnInsert name/location
//       e. If userEmailId present -> upsert user keyed by emailId + orgId (or emailId alone) with orgId set
//       f. Track counts: organisationsInserted, organisationsUpdated (if AddToSet modified), heatNetworksInserted, usersInserted
//    - Collect any parsing errors into result
//    - Return ImportResult with counts and error details
// 4. Create an API controller `ImportController` with POST action `UploadCsv`:
//    - Route: POST /api/import/upload-csv
//    - Accept IFormFile `file` (multipart/form-data)
//    - Validate file present and size > 0
//    - Call csvImportService.ImportFromCsvAsync(file)
//    - Return 200 OK with ImportResult or 400 on invalid input
//
// Notes:
// - Use simple CSV parsing (split on ','), trim quotes. This is minimal; for production consider CsvHelper.
// - Use BsonDocument collections to avoid tight model coupling.
// - No distributed transaction is used; each upsert is independent.
// - Code is written to be dependency-injectable and testable.


using Microsoft.AspNetCore.Mvc;
using HNTAS.Core.Api.Services;

namespace HNTAS.Core.Api.Controllers
{
    public class ImportResult
    {
        public int RowsProcessed { get; set; }
        public int OrganisationsInserted { get; set; }
        public int OrganisationsUpdated { get; set; }
        public int HeatNetworksInserted { get; set; }
        public int HeatNetworksUpdated { get; set; }
        public int UsersInserted { get; set; }
        public int UsersUpdated { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }    

    [ApiController]
    [Route("api/[controller]")]
    public class ImportController : ControllerBase
    {
        private readonly ICsvImportService _csvImportService;
        private readonly ILogger<ImportController> _logger;

        public ImportController(ICsvImportService csvImportService, ILogger<ImportController> logger)
        {
            _csvImportService = csvImportService;
            _logger = logger;
        }

        /// <summary>
        /// Upload a CSV file and import rows into Organisations, HeatNetworks and Users collections.
        /// Expected CSV headers: hnId, hnName, hnLocation, organisationId, organisationName, userEmailId
        /// Existing route kept for compatibility.
        /// </summary>
        [HttpPost("upload-csv")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadCsv([FromForm(Name = "file")] IFormFile file, CancellationToken ct)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No file provided or file is empty." });
            }

            try
            {
                var result = await _csvImportService.ImportFromCsvAsync(file, ct);
                return Ok(result);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("CSV import cancelled by request.");
                return StatusCode(StatusCodes.Status499ClientClosedRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while importing CSV.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }

        }        
    }
}