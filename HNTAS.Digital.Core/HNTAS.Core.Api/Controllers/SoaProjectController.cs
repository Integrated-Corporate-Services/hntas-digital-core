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
        public async Task<IActionResult> CreateProject([FromQuery] string hnId)
        {
            _logger.LogInformation("Creating new SOA project for Heat Network ID: {HeatNetworkId}", hnId);

            if (string.IsNullOrEmpty(hnId))
            {
                _logger.LogWarning("Heat Network ID is missing in create request.");
                return BadRequest("Heat Network ID is required.");
            }

            var newProject = await _soaProjectService.CreateAsync(hnId);

            _logger.LogInformation("SOA project created successfully with ID: {ProjectId}", newProject.Id);
            return CreatedAtAction(nameof(Get), new { projectId = newProject.Id }, newProject);
        }

        [HttpPatch("connections")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateConnections([FromBody] UpdateConnectionsRequest updateConnectionsRequest)
        {
            _logger.LogInformation("Updating connection types for Heat Network ID: {HeatNetworkId}", updateConnectionsRequest.HnId);

            if (string.IsNullOrEmpty(updateConnectionsRequest.HnId))
            {
                _logger.LogWarning("Heat Network ID is missing in connection update.");
                return BadRequest("Heat Network ID is required.");
            }

            var project = await _soaProjectService.GetByHeatNetworkIdAsync(updateConnectionsRequest.HnId);
            if (project == null)
            {
                _logger.LogWarning("SOA project not found for connection update: {HeatNetworkId}", updateConnectionsRequest.HnId);
                return NotFound();
            }

            await _soaProjectService.UpdateConnectionTypesAsync(updateConnectionsRequest.HnId, updateConnectionsRequest.ConnectionTypes);
            _logger.LogInformation("Connection types updated for Heat Network ID: {HeatNetworkId}", updateConnectionsRequest.HnId);

            return Ok();
        }

        [HttpPatch("network-type")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateNetworkType([FromQuery] string hnId, [FromBody] NetworkTypeSelection networkTypeSelection)
        {
            _logger.LogInformation("Updating network type for Heat Network ID: {HeatNetworkId}", hnId);

            if (string.IsNullOrEmpty(hnId))
            {
                _logger.LogWarning("Heat Network ID is missing in network type update.");
                return BadRequest("Heat Network ID is required.");
            }

            var project = await _soaProjectService.GetByHeatNetworkIdAsync(hnId);
            if (project == null)
            {
                _logger.LogWarning("SOA project not found for network type update: {HeatNetworkId}", hnId);
                return NotFound();
            }

            await _soaProjectService.UpdateNetworkTypeAsync(hnId, networkTypeSelection);
            _logger.LogInformation("Network type updated for Heat Network ID: {HeatNetworkId}", hnId);

            return Ok();
        }
    }
}
