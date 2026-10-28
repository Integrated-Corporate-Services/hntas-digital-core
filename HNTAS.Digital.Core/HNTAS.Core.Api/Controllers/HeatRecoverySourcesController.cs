using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HNTAS.Core.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class HeatRecoverySourcesController : ControllerBase
    {
        private readonly ILogger<HeatRecoverySourcesController> _logger;
        private readonly IHeatRecoverySourceService _heatRecoverySourceService;
        public HeatRecoverySourcesController(ILogger<HeatRecoverySourcesController> logger, IHeatRecoverySourceService heatRecoverySourceService)
        {
            _logger = logger;
            _heatRecoverySourceService = heatRecoverySourceService;
        }

        /// <summary>
        /// Retrieves a list of all Heat Recovery sources.
        /// </summary>
        /// <returns>
        /// A list of <see cref="HeatRecovery"/> objects wrapped in an HTTP 200 OK response.
        /// Returns HTTP 500 Internal Server Error if the retrieval fails.
        /// </returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<HeatRecovery>))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<HeatRecovery>>> GetAllAsync()
        {
            try
            {
                var results = await _heatRecoverySourceService.GetAllAsync();
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve Heat Recovery sources.");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
