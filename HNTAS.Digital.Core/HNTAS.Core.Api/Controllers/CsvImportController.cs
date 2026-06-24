using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Services;
using Microsoft.AspNetCore.Mvc;

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
        private readonly IEmailService _emailService;

        public ImportController(ICsvImportService csvImportService, ILogger<ImportController> logger, IEmailService emailService)
        {
            _csvImportService = csvImportService;
            _logger = logger;
            _emailService = emailService;
        }        

        [HttpPost("upload-csv")]
        [Consumes("text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ImportResult))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status499ClientClosedRequest)]
        public async Task<ActionResult<ImportResult>> UploadCsv(string fileContent, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(fileContent))
            {
                return BadRequest(new { error = "No file provided or file is empty." });
            }

            try
            {
                var result = await _csvImportService.ImportFromCsvAsync(fileContent, ct);

                // Notification for existing orgs/users
                if (result.DataForExistingOrgOrUser.Any())
                {
                    foreach (var item in result.DataForExistingOrgOrUser)
                    {
                        await _emailService.TrySendOfgemDataForExistingOrgOrRpEmailAsync(item);
                    }
                }

                // Notification for new RPs
                if (result.DataForNewOrgOrUser.Any())
                {
                    foreach (var item in result.DataForNewOrgOrUser)
                    {
                        await _emailService.TrySendOfgemDataForNewRpEmailAsync(item);
                    }
                }

                // Removing the DataForExistingOrgOrUser and DataForNewOrgOrUser from the result before returning to the client
                result.DataForExistingOrgOrUser.Clear();
                result.DataForNewOrgOrUser.Clear();
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