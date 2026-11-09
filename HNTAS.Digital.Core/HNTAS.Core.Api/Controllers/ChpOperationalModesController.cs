using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HNTAS.Core.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ChpOperationalModesController : ControllerBase
    {
        private readonly ILogger<ChpOperationalModesController> _logger;
        private readonly IChpOperationalModeService _chpOperationalModeService;
        public ChpOperationalModesController(ILogger<ChpOperationalModesController> logger, IChpOperationalModeService chpOperationalModeService)
        {
            _logger = logger;
            _chpOperationalModeService = chpOperationalModeService;
        }

        /// <summary>
        /// Retrieves a list of all CHP operational modes.
        /// </summary>
        /// <returns>
        /// A list of <see cref="CHPOperationalMode"/> objects wrapped in an HTTP 200 OK response.
        /// Returns HTTP 500 Internal Server Error if the retrieval fails.
        /// </returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<CHPOperationalMode>))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<CHPOperationalMode>>> GetAllAsync()
        {
            try
            {
                var results = await _chpOperationalModeService.GetAllAsync();
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve CHP operational modes.");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
