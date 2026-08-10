using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models;
using HNTAS.Core.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace HNTAS.Core.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImportController : ControllerBase
    {
        private readonly ICsvImportService _csvImportService;
        private readonly IUserService  _userService;
        private readonly ILogger<ImportController> _logger;
        private readonly IEmailService _emailService;

        public ImportController(ICsvImportService csvImportService, IUserService userService, ILogger<ImportController> logger, IEmailService emailService)
        {
            _csvImportService = csvImportService;
            _userService = userService;
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
                    // update the associated network managers with new heat network data if any heat networks were inserted or updated                
                    foreach(var item in result.DataForExistingOrgOrUser)
                    {
                        var rpEmailId = item.UserEmailId;
                        var heatNetworkIds = item.HeatNetworkIds;
                        var rpUser = await _userService.GetByEmailAsync(rpEmailId);
                        var networkManagers = await _userService.GetActiveNetworkManagersByRpUserIdAsync(rpUser.Id);
                        if(networkManagers == null)
                        {
                            break;
                        }
                        foreach (var networkManager in networkManagers) 
                        {
                            foreach (var heatNetworkId in heatNetworkIds)
                            {
                                await _userService.UpdateUserNetwork(networkManager.Id, heatNetworkId, ContributorRole.NetworkManager);
                                _logger.LogInformation("Updated associated network managers with new heat network data for {heatNetworkId} - {networkManger}", heatNetworkId, networkManager.EmailId);
                            }
                        }                        
                    }
                    _logger.LogInformation("Updated associated network managers with new heat network data.");
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