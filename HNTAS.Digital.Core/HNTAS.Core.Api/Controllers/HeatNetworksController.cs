using AutoMapper;
using HNTAS.Core.Api.Constants;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Helpers;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models;
using HNTAS.Core.Api.Models.NetworkDetails;
using HNTAS.Core.Api.Models.Soa;
using HNTAS.Core.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace HNTAS.Core.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public partial class HeatNetworksController : ControllerBase
    {
        private readonly IHeatNetworkService _hnService;
        private readonly ILogger<HeatNetworksController> _logger;
        private readonly ICounterService _counterService;
        private readonly IMapper _mapper;
        private readonly IAuditService _auditService;

        public HeatNetworksController(IHeatNetworkService hnService, ILogger<HeatNetworksController> logger, ICounterService counterService, IMapper mapper, IAuditService auditService)
        {
            _hnService = hnService;
            _logger = logger;
            _counterService = counterService;
            _mapper = mapper;
            _auditService = auditService;
        }

        /// <summary>
        /// Retrieves a list of all heat networks available in the system.
        /// </summary>
        /// <returns>A list of heat network response objects.</returns>
        [HttpGet] // This defines the route as GET /api/HeatNetworks
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<HeatNetworkResponse>))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<HeatNetworkResponse>>> GetHeatNetworks()
        {
            _logger.LogInformation("Attempting to retrieve all heat networks.");
            try
            {
                var heatNetworks = await _hnService.GetAsync();
                var heatNetworksResponse = _mapper.Map<List<HeatNetworkResponse>>(heatNetworks);
                _logger.LogInformation("Successfully retrieved {HeatNetworkCount} heatNetworks.", heatNetworks.Count);

                return Ok(heatNetworksResponse); // Returns 200 OK with the list of heat networks
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving all heat networks.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while retrieving heat networks.");
            }
        }

        [HttpGet("hnIds")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(List<HeatNetworkResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<HeatNetworkResponse>>> GetHeatNetworksByHnIds([FromQuery] string hnIdsString)
        {
            // Split the comma-separated string of IDs into a List<string>
            List<string> hnIds = hnIdsString?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>();

            // Validate input IDs
            if (hnIds == null || !hnIds.Any())
            {
                _logger.LogWarning("GetHeatNetworksByIds called with no IDs provided in the query string.");
                return BadRequest("Please provide at least one heat network ID in the query string (e.g., /api/heatnetwork/list?ids=id1,id2).");
            }

            try
            {
                var heatNetworks = await _hnService.GetByHnIdsAsync(hnIds);

                if (heatNetworks == null || !heatNetworks.Any())
                {
                    _logger.LogInformation("No heat networks found for the provided IDs: {HeatNetworkIds}", string.Join(", ", hnIds));
                    return NotFound("No heat networks found for the given IDs.");
                }

                var heatNetworksResponse = _mapper.Map<List<HeatNetworkResponse>>(heatNetworks);

                return Ok(heatNetworksResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving heat networks for IDs: {HeatNetworkIds}", string.Join(", ", hnIds));
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while retrieving heat networks.");
            }
        }

        [HttpGet("{hnId}")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(HeatNetworkResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<HeatNetworkResponse>> GetHeatNetworkByHnId(string hnId)
        {
            // Validate input ID
            if (string.IsNullOrEmpty(hnId))
            {
                _logger.LogWarning("GetHeatNetworkByHnId called with a null or empty ID.");
                return BadRequest("Please provide a valid heat network ID in the URL.");
            }

            try
            {
                var heatNetwork = await _hnService.GetByHnIdAsync(hnId);

                if (heatNetwork == null)
                {
                    _logger.LogInformation("No heat network found for the provided ID: {HeatNetworkId}", StringFormatter.Sanitize(hnId));
                    return NotFound("No heat network found for the given ID.");
                }

                var heatNetworkResponse = _mapper.Map<HeatNetworkResponse>(heatNetwork);

                return Ok(heatNetworkResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving heat network for ID: {HeatNetworkId}", StringFormatter.Sanitize(hnId));
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while retrieving the heat network.");
            }
        }



        [HttpPost("add-heat-network")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(HeatNetworkResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<HeatNetworkResponse>> AddHeatNetwork([FromBody] HeatNetwork heatNetworkDetails)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(heatNetworkDetails.HnId))
                {
                    var sequenceID = await _counterService.GetNextSequenceValue("heatNetworkId_sequence");
                    var heatNetworkId = $"HN{sequenceID:D7}";
                    heatNetworkDetails.HnId = heatNetworkId;
                    heatNetworkDetails.UHnId = sequenceID.ToString();
                    _logger.LogInformation("Generated new heat network ID: {HeatNetworkId}", heatNetworkDetails.HnId);
                }

                await _hnService.CreateAsync(heatNetworkDetails, true);
                _logger.LogInformation("New heat network initially registered: {HNID} (DB Id: {Id})", heatNetworkDetails.HnId, heatNetworkDetails.Id);

                return CreatedAtAction(nameof(AddHeatNetwork), new { id = heatNetworkDetails.Id }, heatNetworkDetails);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Internal Server Error",
                    Detail = "An unexpected error occurred during initial user registration."
                });
            }
        }

        /// <summary>
        /// Updates the NetworkElements for a given heat network identified by HnId.
        /// </summary>
        [HttpPut("network-elements")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(HeatNetworkResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<HeatNetworkResponse>> UpdateNetworkElements([FromBody] NetworkElements request, string hnId)
        {
            if (string.IsNullOrWhiteSpace(hnId))
            {
                _logger.LogWarning("UpdateNetworkElements called with empty HnId.");
                return BadRequest("Please provide a valid heat network HnId.");
            }

            if (request == null)
            {
                _logger.LogWarning("UpdateNetworkElements called without NetworkElements payload for HnId: {HnId}", StringFormatter.Sanitize(hnId));
                return BadRequest("Please provide NetworkElements to update.");
            }

            try
            {
                var existingHeatNetwork = await _hnService.GetByHnIdAsync(hnId);
                if (existingHeatNetwork == null)
                {
                    _logger.LogInformation("No heat network found for HnId: {HnId}", StringFormatter.Sanitize(hnId));
                    return NotFound($"No heat network found for HnId '{hnId}'.");
                }
                
                var existingHeatNetworkSnapshot = System.Text.Json.JsonSerializer.Deserialize<HeatNetwork>(
                    System.Text.Json.JsonSerializer.Serialize(existingHeatNetwork)
                )!;

                existingHeatNetwork.NetworkElements = request;
                await _hnService.UpdateAsync(hnId, existingHeatNetwork);

                // Only log an audit event if NetworkElements were previously null, to capture the addition of elements rather than updates to existing elements
                var isRegistrationEnabledString = Environment.GetEnvironmentVariable("IS_REGISTRATION_ENABLED");
                if (!string.IsNullOrEmpty(isRegistrationEnabledString) &&
                    isRegistrationEnabledString.ToLower() == "true" && existingHeatNetworkSnapshot.NetworkElements == null)
                {
                    await _auditService.SaveAuditAsync<HeatNetwork>(
                        entryType: HeatNetworkEvents.NetworkElementsAdded,
                        actorId: existingHeatNetwork.CreatedBy,
                        entityId: existingHeatNetwork.HnId!,
                        oldState: existingHeatNetworkSnapshot,
                        newState: existingHeatNetwork,
                        elementName: "All Elements",
                        phase: existingHeatNetwork.Phase,
                        stage: HeatNetworkHelper.GetStageFromPhase(existingHeatNetwork.Phase)
                        );
                }                    
                
                _logger.LogInformation("Updated NetworkElements for HnId: {HnId}", StringFormatter.Sanitize(hnId));
                var response = CreatedAtAction(nameof(UpdateNetworkElements), new { id = existingHeatNetwork.Id }, existingHeatNetwork);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating NetworkElements for HnId: {HnId}", StringFormatter.Sanitize(hnId));
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while updating the heat network.");
            }
        }

        [HttpPatch("network-details-document-update")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SaveDocument([FromBody] NetworkDetailsUploadDocumentRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid SaveDocument request: {@Errors}",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Saving {DocumentType} document for HN ID: {HnId}, UploadedBy: {UploadedBy}",
                request.DocumentType, request.HnId, request.UploadedBy);

            

            var document = new NetworkDetailsDocument
            {
                FileName = request.FileName,
                S3Key = request.S3Key,                
                UploadedAt = DateTime.UtcNow,
                UploadedBy = request.UploadedBy
            };

            try
            {
                switch (request.DocumentType)
                {                    
                    case DocumentType.MeteringAndMonitoringStrategy:
                        await _hnService.UpdateMeteringAndMonitoringStrategyAsync(request.HnId, document);
                        break;
                    case DocumentType.AssessmentPlan:
                        await _hnService.UpdateAssessmentPlanAsync(request.HnId, document);
                        break;
                    case DocumentType.DesignConstructionLog:
                        await _hnService.UpdateDesignConstructionLogAsync(request.HnId, document);
                        break;
                    default:
                        _logger.LogWarning("Unsupported document type: {DocumentType}", request.DocumentType);
                        return BadRequest($"Unsupported document type: {request.DocumentType}");
                }

                _logger.LogInformation("{DocumentType} document saved successfully for HN ID: {HnId}, UploadedBy: {UploadedBy}",
                    request.DocumentType, request.HnId, request.UploadedBy);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save {DocumentType} document for HN ID: {HnId}, UploadedBy: {UploadedBy}",
                    request.DocumentType, request.HnId, request.UploadedBy);
                throw;
            }
        }

    }
}
