using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models.Soa;
using Microsoft.AspNetCore.Mvc;

namespace HNTAS.Core.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SOAController : ControllerBase
    {
        private readonly ISoaService _soaService;
        private readonly ILogger<SOAController> _logger;

        public SOAController(ISoaService soaProjectService, ILogger<SOAController> logger)
        {
            _soaService = soaProjectService;
            _logger = logger;
        }


        [HttpGet("heat-network/{hnId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Soa))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Soa>> GetByHeatNetworkIdAsync(string hnId)
        {
            _logger.LogInformation("Retrieving SOA project for Heat Network ID: {HnId}", hnId);

            var project = await _soaService.GetByHeatNetworkIdAsync(hnId);

            if (project is null)
            {
                _logger.LogWarning("SOA project not found for Heat Network ID: {HnId}", hnId);
                return NotFound();
            }

            _logger.LogInformation("SOA project retrieved successfully for Heat Network ID: {HnId}", hnId);
            return Ok(project);
        }


        [HttpPost("create")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Soa))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateProject([FromQuery] string hnId, [FromQuery] string createdBy)
        {
            if (string.IsNullOrWhiteSpace(hnId))
            {
                _logger.LogWarning("Create request rejected: missing Heat Network ID.");
                return BadRequest("Heat Network ID is required.");
            }

            if (string.IsNullOrWhiteSpace(createdBy))
            {
                _logger.LogWarning("Create request rejected: missing CreatedBy.");
                return BadRequest("CreatedBy is required.");
            }

            _logger.LogInformation("Initiating SOA creation for Heat Network ID: {HnId} by user: {CreatedBy}", hnId, createdBy);

            var soa = await _soaService.CreateAsync(hnId, createdBy);

            if (soa is null)
            {
                _logger.LogWarning("SOA creation failed for Heat Network ID: {HnId}", hnId);
                return BadRequest("Unable to create SOA data.");
            }

            _logger.LogInformation("SOA data successfully created for Heat Network ID: {HnId}", hnId);
            return Ok(soa);
        }




        [HttpPatch("connections")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateConnections([FromBody] UpdateConnectionsRequest request)
        {
            _logger.LogInformation("Updating connection types for Heat Network ID: {HnId} by {UpdatedBy}", request.HnId, request.UpdatedBy);

            if (string.IsNullOrEmpty(request.HnId))
            {
                _logger.LogWarning("Heat Network ID is missing in connection update.");
                return BadRequest("Heat Network ID is required.");
            }

            if (string.IsNullOrEmpty(request.UpdatedBy))
            {
                _logger.LogWarning("UpdatedBy is missing in connection update.");
                return BadRequest("UpdatedBy is required.");
            }

            var project = await _soaService.GetByHeatNetworkIdAsync(request.HnId);
            if (project == null)
            {
                _logger.LogWarning("SOA project not found for connection update: {HnId}", request.HnId);
                return NotFound();
            }

            await _soaService.UpdateConnectionTypesAsync(request.HnId, request.UpdatedBy, request.ConnectionTypes);
            _logger.LogInformation("Connection types updated for Heat Network ID: {HnId}", request.HnId);

            return Ok();
        }


        [HttpPatch("network-type")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateNetworkType([FromQuery] string hnId, [FromQuery] string updatedBy, [FromBody] NetworkTypeSelection networkTypeSelection)
        {
            _logger.LogInformation("Updating network type for Heat Network ID: {hnId} by {UpdatedBy}", hnId, updatedBy);

            if (string.IsNullOrEmpty(hnId))
            {
                _logger.LogWarning("Heat Network ID is missing in network type update.");
                return BadRequest("Heat Network ID is required.");
            }

            if (string.IsNullOrEmpty(updatedBy))
            {
                _logger.LogWarning("UpdatedBy is missing in network type update.");
                return BadRequest("UpdatedBy is required.");
            }

            var project = await _soaService.GetByHeatNetworkIdAsync(hnId);
            if (project == null)
            {
                _logger.LogWarning("SOA project not found for network type update: {hnId}", hnId);
                return NotFound();
            }

            await _soaService.UpdateNetworkTypeAsync(hnId, updatedBy, networkTypeSelection);
            _logger.LogInformation("Network type updated for Heat Network ID: {hnId}", hnId);

            return Ok();
        }

        [HttpPatch("network-elements")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateNetworkElements([FromQuery] string hnId, [FromQuery] string updatedBy, [FromBody] List<HeatNetworkElement> networkElements)
        {
            _logger.LogInformation("Updating network type for Heat Network ID: {hnId} by {UpdatedBy}", hnId, updatedBy);

            if (string.IsNullOrEmpty(hnId))
            {
                _logger.LogWarning("Heat Network ID is missing in network type update.");
                return BadRequest("Heat Network ID is required.");
            }

            if (string.IsNullOrEmpty(updatedBy))
            {
                _logger.LogWarning("UpdatedBy is missing in network type update.");
                return BadRequest("UpdatedBy is required.");
            }

            var project = await _soaService.GetByHeatNetworkIdAsync(hnId);
            if (project == null)
            {
                _logger.LogWarning("SOA project not found for network type update: {hnId}", hnId);
                return NotFound();
            }

            await _soaService.UpdateHeatNetworkElementsAsync(hnId, networkElements, updatedBy);
            _logger.LogInformation("Network type updated for Heat Network ID: {hnId}", hnId);

            return Ok();
        }

        [HttpPost("element-locations")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SaveElementLocations([FromBody] UpdateElementLocationsRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid SaveLocations request: {@Errors}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Saving locations for element type: {ElementType} in HN ID: {HnId} by {UpdatedBy}. Location count: {LocationCount}",
                request.ElementType, request.HnId, request.UpdatedBy, request.Locations?.Count ?? 0);

            var project = await _soaService.GetByHeatNetworkIdAsync(request.HnId);
            if (project == null)
            {
                _logger.LogWarning("SOA project not found for location save: {HnId}", request.HnId);
                return NotFound();
            }

            try
            {
                await _soaService.UpdateElementLocationsAsync(request.HnId, request.ElementType, request.Locations, request.UpdatedBy);
                _logger.LogInformation("Locations updated successfully for element type: {ElementType} in HN ID: {HnId} by {UpdatedBy}",
                    request.ElementType, request.HnId, request.UpdatedBy);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update locations for element type: {ElementType} in HN ID: {HnId} by {UpdatedBy}",
                    request.ElementType, request.HnId, request.UpdatedBy);
                throw;
            }
        }

        [HttpPost("element-documents")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SaveElementDocuments([FromBody] UpdateElementDocumentsRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid SaveDocuments request: {@Errors}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Saving documents for element type: {ElementType} in HN ID: {HnId} by {UpdatedBy}. Document count: {DocumentCount}",
                request.ElementType, request.HnId, request.UpdatedBy, request.Documents?.Count ?? 0);

            var project = await _soaService.GetByHeatNetworkIdAsync(request.HnId);
            if (project == null)
            {
                _logger.LogWarning("SOA project not found for document save: {HnId}", request.HnId);
                return NotFound();
            }

            try
            {
                await _soaService.UpdateElementDocumentsAsync(request.HnId, request.ElementType, request.Documents, request.UpdatedBy);
                _logger.LogInformation("Documents updated successfully for element type: {ElementType} in HN ID: {HnId} by {UpdatedBy}",
                    request.ElementType, request.HnId, request.UpdatedBy);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update documents for element type: {ElementType} in HN ID: {HnId} by {UpdatedBy}",
                    request.ElementType, request.HnId, request.UpdatedBy);
                throw;
            }
        }


        [HttpPost("assessment-plan")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SaveAssessmentPlan([FromBody] UpdateAssessmentPlanRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid SaveAssessmentPlan request: {@Errors}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Saving assessment plan for HN ID: {HnId}, Phase: {Phase}, Stage: {Stage}, UploadedBy: {UpdatedBy}",
                request.HnId, request.Phase, request.Stage, request.UpdatedBy);

            var project = await _soaService.GetByHeatNetworkIdAsync(request.HnId);
            if (project == null)
            {
                _logger.LogWarning("SOA project not found for assessment plan save: {HnId}", request.HnId);
                return NotFound();
            }

            var document = new AssessmentPlanDocument
            {
                FileName = request.FileName,
                S3Key = request.S3Key,
                Phase = request.Phase,
                Stage = request.Stage,
                UploadedAt = DateTime.UtcNow,
                UploadedBy = request.UpdatedBy
            };

            try
            {
                await _soaService.UpdateAssessmentPlanDocumentAsync(request.HnId, document);
                _logger.LogInformation("Assessment plan saved successfully for HN ID: {HnId}, Phase: {Phase}, Stage: {Stage}, UploadedBy: {UpdatedBy}",
                    request.HnId, request.Phase, request.Stage, request.UpdatedBy);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save assessment plan for HN ID: {HnId}, Phase: {Phase}, Stage: {Stage}, UploadedBy: {UpdatedBy}",
                    request.HnId, request.Phase, request.Stage, request.UpdatedBy);
                throw;
            }
        }

        [HttpDelete("{hnId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteSoaProject(string hnId)
        {
            if (string.IsNullOrWhiteSpace(hnId))
            {
                _logger.LogWarning("Delete request received with empty HN ID.");
                return BadRequest("Heat Network ID is required.");
            }

            _logger.LogInformation("Attempting to delete SOA project for HN ID: {HnId}", hnId);

            var project = await _soaService.GetByHeatNetworkIdAsync(hnId);
            if (project == null)
            {
                _logger.LogWarning("SOA project not found for deletion: {HnId}", hnId);
                return NotFound();
            }

            try
            {
                await _soaService.DeleteByHeatNetworkIdAsync(hnId);
                _logger.LogInformation("SOA project deleted successfully for HN ID: {HnId}", hnId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete SOA project for HN ID: {HnId}", hnId);
                throw;
            }
        }


        [HttpPut("update-soa-status")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateSoaStatus([FromBody] UpdateSoaStatusRequest request)
        {
            _logger.LogInformation("Updating SOA status to {Status} for HN ID: {HnId} by {UpdatedBy}", request.Status, request.HnId, request.UpdatedBy);

            if (string.IsNullOrWhiteSpace(request.HnId))
                return BadRequest("Heat Network ID is required.");

            if (string.IsNullOrWhiteSpace(request.UpdatedBy))
                return BadRequest("UpdatedBy is required.");

            if (!Enum.IsDefined(typeof(SoaStatus), request.Status))
                return BadRequest($"Invalid SOA status: {request.Status}");

            var soa = await _soaService.UpdateStatusAsync(request.HnId, request.Status, request.UpdatedBy);

            if (soa == null)
            {
                _logger.LogWarning("No SOA found to update for HN ID: {HnId}", request.HnId);
                return BadRequest("SOA not found.");
            }

            return NoContent();
        }

    }
}
