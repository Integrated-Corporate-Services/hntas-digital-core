using HNTAS.Core.Api.Data.Models.External;
using Microsoft.AspNetCore.Mvc;

namespace HNTAS.Core.Api.Controllers
{
    public partial class HeatNetworksController : ControllerBase
    {
        [HttpGet("/api/external/heat-networks")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<HeatNetworkExternalResponse>))]
        public async Task<ActionResult<List<HeatNetworkExternalResponse>>> GetExternalHeatNetworks()
        {
            _logger.LogInformation("External API: Retrieving all heat networks.");
            try
            {
                var networks = await _hnService.GetDetailsAsync();
                return Ok(networks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetExternalHeatNetworks");
                return StatusCode(500, "Internal Server Error");
            }
        }

        [HttpGet("/api/external/heat-network/{hnId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(HeatNetworkExternalResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<HeatNetworkExternalResponse>> GetExternalHeatNetworkById(string hnId)
        {
            _logger.LogInformation("External API: Retrieving heat network {HnId}", hnId);
            try
            {
                var network = await _hnService.GetDetailsByHnIdAsync(hnId);
                if (network == null) return NotFound();

                return Ok(network);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetExternalHeatNetworkById for {HnId}", hnId);
                return StatusCode(500, "Internal Server Error");
            }
        }

        /// <summary>
        /// Updated to GOV.UK parameter naming conventions (snake_case)
        /// URL: GET /api/external/heat-networks/search?from_date=2026-01-01&to_date=2026-01-15
        /// </summary>
        [HttpGet("/api/external/heat-networks/search")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<HeatNetworkExternalResponse>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<HeatNetworkExternalResponse>>> GetExternalHeatNetworksByDate(
            [FromQuery(Name = "from_date")] DateTime fromDate,
            [FromQuery(Name = "to_date")] DateTime toDate)
        {
            // GOV.UK Standards recommend clear error messages for validation
            if (fromDate > toDate)
                return BadRequest("The 'from_date' cannot be after the 'to_date'.");

            _logger.LogInformation("External API: Searching networks between {From} and {To}", fromDate, toDate);
            try
            {
                var networks = await _hnService.GetDetailsByDateRangeAsync(fromDate, toDate);
                return Ok(networks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetExternalHeatNetworksByDate");
                return StatusCode(500, "Internal Server Error");
            }
        }
    }
}