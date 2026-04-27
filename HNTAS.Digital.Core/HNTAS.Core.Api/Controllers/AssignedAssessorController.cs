using HNTAS.Core.Api.Helpers;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models.AssignedAssessor;
using Microsoft.AspNetCore.Mvc;

namespace HNTAS.Core.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AssignedAssessorController : ControllerBase
    {
        private readonly ILogger<AssignedAssessorController> _logger;
        private readonly IHeatNetworkService _heatNetworkService;

        public AssignedAssessorController(ILogger<AssignedAssessorController> logger, IHeatNetworkService heatNetworkService)
        {
            _logger = logger;
            _heatNetworkService = heatNetworkService;
        }

        [HttpGet("assigned-assessor")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AssignedAssessorResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AssignedAssessorResponse>> GetAssignedAssessors(AssignedAssessorRequest request)
        {
            try
            {
                _logger.LogInformation("Retrieving assigned assessors for the heat network(s)");
                var result = await _heatNetworkService.GetAssignedAssessors(request);
                if (result is null)
                {
                    _logger.LogWarning("No assigned assessors found for the heat network(s)");
                    return NotFound();
                }
                _logger.LogInformation("Assigned assessors retrieved successfully for the heat network(s)");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve assigned assessors for the heat network(s)");
                throw;
            }
        }
    }
}
