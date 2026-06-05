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
        public List<OfgemDataModelForNotification> DataForExistingOrgOrUser { get; set; } = new List<OfgemDataModelForNotification>();
        public List<OfgemDataModelForNotification> DataForNewOrgOrUser { get; set; } = new List<OfgemDataModelForNotification>();
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

                // TODO: Email notification to be implemented
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