using HNTAS.Core.Api.Models.Soa;
using Microsoft.AspNetCore.Mvc;

namespace HNTAS.Core.Api.Controllers
{
    public partial class HeatNetworksController : ControllerBase
    {
        [HttpGet("api/external/heat-networks")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<HeatNetworkResponse>))]
        public async Task<ActionResult<List<HeatNetworkResponse>>> GetExternalHeatNetworks()
        {
            _logger.LogInformation("External API: Retrieving all heat networks.");
            try
            {
                var networks = await _hnService.GetAsync();
                return Ok(_mapper.Map<List<HeatNetworkResponse>>(networks));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetExternalHeatNetworks");
                return StatusCode(500, "Internal Server Error");
            }
        }

        [HttpGet("api/external/heat-network/{hnId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(HeatNetworkResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<HeatNetworkResponse>> GetExternalHeatNetworkById(string hnId)
        {
            _logger.LogInformation("External API: Retrieving heat network {HnId}", hnId);
            try
            {
                var network = await _hnService.GetByHnIdAsync(hnId);
                if (network == null) return NotFound();

                return Ok(_mapper.Map<HeatNetworkResponse>(network));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetExternalHeatNetworkById for {HnId}", hnId);
                return StatusCode(500, "Internal Server Error");
            }
        }

        /// <summary>
        /// Updated to GOV.UK parameter naming conventions (snake_case)
        /// URL: GET /external/heat-networks/search?from_date=2026-01-01&to_date=2026-01-15 - YYYY-MM-DD
        /// </summary>
        [HttpGet("api/external/heat-networks/search")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<HeatNetworkResponse>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<HeatNetworkResponse>>> GetExternalHeatNetworksByDate(
           [FromQuery] DateTime fromDate,
           [FromQuery] DateTime toDate)
        {
            // GOV.UK Standards recommend clear error messages for validation
            if (fromDate > toDate)
                return BadRequest("The 'fromDate' cannot be after the 'toDate'.");

            _logger.LogInformation("External API: Searching networks between {From} and {To}", fromDate, toDate);
            try
            {
                // Ensure the service call handles the full day of 'toDate'
                var networks = await _hnService.GetByDateRangeAsync(fromDate, toDate);
                return Ok(_mapper.Map<List<HeatNetworkResponse>>(networks));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetExternalHeatNetworksByDate");
                return StatusCode(500, "Internal Server Error");
            }
        }
    }
}