using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models.Soa;
using Microsoft.AspNetCore.Mvc;

namespace HNTAS.Core.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SoaProjectController : ControllerBase
    {
        private readonly ISoaProjectService _soaProjectService;
        private readonly ILogger<SoaProjectController> _logger;

        public SoaProjectController(ISoaProjectService soaProjectService, ILogger<SoaProjectController> logger)
        {
            _soaProjectService = soaProjectService;
            _logger = logger;
        }

        [HttpGet("{projectId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SoaProject))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SoaProject>> Get(string projectId)
        {
            _logger.LogInformation("Retrieving SOA project with ID: {ProjectId}", projectId);

            var project = await _soaProjectService.GetByIdAsync(projectId);

            if (project == null)
            {
                _logger.LogWarning("SOA project not found: {ProjectId}", projectId);
                return NotFound();
            }

            _logger.LogInformation("SOA project retrieved successfully: {ProjectId}", projectId);
            return Ok(project);
        }


        [HttpGet("heat-network/{hnId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SoaProject))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SoaProject>> GetByHeatNetworkIdAsync(string hnId)
        {
            _logger.LogInformation("Retrieving SOA project for Heat Network ID: {HeatNetworkId}", hnId);

            var project = await _soaProjectService.GetByHeatNetworkIdAsync(hnId);

            if (project == null)
            {
                _logger.LogWarning("SOA project not found for Heat Network ID: {HeatNetworkId}", hnId);
                return NotFound();
            }

            _logger.LogInformation("SOA project retrieved successfully for Heat Network ID: {HeatNetworkId}", hnId);
            return Ok(project);
        }

        [HttpPost("create")]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(SoaProject))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateProject([FromQuery] string hnId, [FromQuery] string createdBy)
        {
            _logger.LogInformation("Creating new SOA project for Heat Network ID: {HeatNetworkId} by {UpdatedBy}", hnId, createdBy);

            if (string.IsNullOrEmpty(hnId))
            {
                _logger.LogWarning("Heat Network ID is missing in create request.");
                return BadRequest("Heat Network ID is required.");
            }

            if (string.IsNullOrEmpty(createdBy))
            {
                _logger.LogWarning("UpdatedBy is missing in create request.");
                return BadRequest("UpdatedBy is required.");
            }

            var newProject = await _soaProjectService.CreateAsync(hnId, createdBy);

            _logger.LogInformation("SOA project created successfully with ID: {ProjectId}", newProject.Id);
            return CreatedAtAction(nameof(Get), new { projectId = newProject.Id }, newProject);
        }

        [HttpPatch("connections")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateConnections([FromBody] UpdateConnectionsRequest request)
        {
            _logger.LogInformation("Updating connection types for Heat Network ID: {HeatNetworkId} by {UpdatedBy}", request.HnId, request.UpdatedBy);

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

            var project = await _soaProjectService.GetByHeatNetworkIdAsync(request.HnId);
            if (project == null)
            {
                _logger.LogWarning("SOA project not found for connection update: {HeatNetworkId}", request.HnId);
                return NotFound();
            }

            await _soaProjectService.UpdateConnectionTypesAsync(request.HnId, request.UpdatedBy, request.ConnectionTypes);
            _logger.LogInformation("Connection types updated for Heat Network ID: {HeatNetworkId}", request.HnId);

            return Ok();
        }


        [HttpPatch("network-type")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateNetworkType([FromQuery] string hnId, [FromQuery] string updatedBy, [FromBody] NetworkTypeSelection networkTypeSelection)
        {
            _logger.LogInformation("Updating network type for Heat Network ID: {HeatNetworkId} by {UpdatedBy}", hnId, updatedBy);

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

            var project = await _soaProjectService.GetByHeatNetworkIdAsync(hnId);
            if (project == null)
            {
                _logger.LogWarning("SOA project not found for network type update: {HeatNetworkId}", hnId);
                return NotFound();
            }

            await _soaProjectService.UpdateNetworkTypeAsync(hnId, updatedBy, networkTypeSelection);
            _logger.LogInformation("Network type updated for Heat Network ID: {HeatNetworkId}", hnId);

            return Ok();
        }

        [HttpPatch("network-elements")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateNetworkElements([FromQuery] string hnId, [FromQuery] string updatedBy, [FromBody] List<HeatNetworkElement> networkElements)
        {
            _logger.LogInformation("Updating network type for Heat Network ID: {HeatNetworkId} by {UpdatedBy}", hnId, updatedBy);

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

            var project = await _soaProjectService.GetByHeatNetworkIdAsync(hnId);
            if (project == null)
            {
                _logger.LogWarning("SOA project not found for network type update: {HeatNetworkId}", hnId);
                return NotFound();
            }

            await _soaProjectService.UpdateHeatNetworkElementsAsync(hnId, networkElements, updatedBy);
            _logger.LogInformation("Network type updated for Heat Network ID: {HeatNetworkId}", hnId);

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

            var project = await _soaProjectService.GetByHeatNetworkIdAsync(request.HnId);
            if (project == null)
            {
                _logger.LogWarning("SOA project not found for location save: {HnId}", request.HnId);
                return NotFound();
            }

            try
            {
                await _soaProjectService.UpdateElementLocationsAsync(request.HnId, request.ElementType, request.Locations, request.UpdatedBy);
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

            var project = await _soaProjectService.GetByHeatNetworkIdAsync(request.HnId);
            if (project == null)
            {
                _logger.LogWarning("SOA project not found for document save: {HnId}", request.HnId);
                return NotFound();
            }

            try
            {
                await _soaProjectService.UpdateElementDocumentsAsync(request.HnId, request.ElementType, request.Documents, request.UpdatedBy);
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


    }
}
