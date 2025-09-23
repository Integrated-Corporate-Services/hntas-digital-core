using AutoMapper;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models.Soa;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace HNTAS.Core.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HeatNetworksController : ControllerBase
    {
        private readonly IHeatNetworkService _hnService;
        private readonly ILogger<HeatNetworksController> _logger;
        private readonly ICounterService _counterService;
        private readonly IMapper _mapper;

        public HeatNetworksController(IHeatNetworkService hnService, ILogger<HeatNetworksController> logger, ICounterService counterService, IMapper mapper)
        {
            _hnService = hnService;
            _logger = logger;
            _counterService = counterService;
            _mapper = mapper;
        }


        /// <summary>
        /// Retrieves a list of all users.
        /// </summary>
        /// <returns>A list of user objects.</returns>
        [HttpGet] // This defines the route as GET /api/users
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

                return Ok(heatNetworksResponse); // Returns 200 OK with the list of users
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
                    _logger.LogInformation("No heat network found for the provided ID: {HeatNetworkId}", hnId);
                    return NotFound("No heat network found for the given ID.");
                }

                var heatNetworkResponse = _mapper.Map<HeatNetworkResponse>(heatNetwork);

                return Ok(heatNetworkResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving heat network for ID: {HeatNetworkId}", hnId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while retrieving the heat network.");
            }
        }



        [HttpPost("add-heat-network")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(HeatNetworkResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<HeatNetworkResponse>> AddHeatNetwork([FromBody] HeatNetwork heatNetworkDetails)
        {
            //if (!ModelState.IsValid)
            //{
            //    _logger.LogWarning("Invalid initial registration data for UserId: {UserId}, EmailId: {EmailId}. Errors: {Errors}",
            //        registrationData.OneLoginId, registrationData.EmailId, string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            //    return ValidationProblem(ModelState);
            //}

            try
            {
                if (String.IsNullOrWhiteSpace(heatNetworkDetails.HnId))
                {
                    var sequenceID = await _counterService.GetNextSequenceValue("heatNetworkId_sequence");
                    var heatNetworkId = $"HN{sequenceID:D7}";
                    heatNetworkDetails.HnId = heatNetworkId;
                    _logger.LogInformation("Generated new heat network ID: {HeatNetworkId}", heatNetworkDetails.HnId);

                }

                await _hnService.CreateAsync(heatNetworkDetails);
                _logger.LogInformation("New heat network initially registered: {HNID} (DB Id: {Id})", heatNetworkDetails.HnId, heatNetworkDetails.Id);

                return CreatedAtAction(nameof(AddHeatNetwork), new { id = heatNetworkDetails.Id }, heatNetworkDetails);
            }
            //catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            //{
            //    _logger.LogWarning(ex, "Duplicate key error during initial user registration for UserId: {UserId}, EmailId: {EmailId}", registrationData.OneLoginId, registrationData.EmailId);
            //    return Conflict(new ProblemDetails
            //    {
            //        Status = StatusCodes.Status409Conflict,
            //        Title = "User Already Exists",
            //        Detail = $"A user with the provided UserId ({registrationData.OneLoginId}) or EmailId ({registrationData.EmailId}) already exists."
            //    });
            //}
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Unexpected error during initial user registration for UserId: {UserId}, EmailId: {EmailId}", registrationData.OneLoginId, registrationData.EmailId);
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Internal Server Error",
                    Detail = "An unexpected error occurred during initial user registration."
                });
            }
        }

        // GET: api/HeatNetworks/{hnid}/assessor-impartiality
        [HttpGet("{hnid}/assessor-impartiality")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<bool>> GetAssessorImpartiality(string hnid)
        {
            var heatnetwork = await _hnService.GetByHnIdAsync(hnid);
            if (heatnetwork == null || heatnetwork.Soa.JourneyData == null)
                return NotFound();

            return Ok(heatnetwork.Soa.JourneyData.HasAssessorDeclaredImpartiality);
        }

        // POST: api/HeatNetworks/{hnid}/assessor-impartiality
        [HttpPost("{hnid}/assessor-impartiality")]
        public async Task<ActionResult<bool>> SetAssessorImpartiality(string hnid)
        {
            var updated = await _hnService.UpdateAssessorImpartialityAsync(hnid);
            if (!updated)
                return NotFound();

            return updated;
        }
    }
}
