using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace HNTAS.Core.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuditController : Controller
    {
        private readonly IAuditService _auditService;

        public AuditController(IAuditService auditService)
        {
            _auditService = auditService;
        }

        /// <summary>
        /// Gets the audit history for a specific Heat Network.
        /// </summary>
        /// <param name="hnId">The Heat Network ID (EntityId)</param>
        /// <returns>A list of formatted audit logs for the UK UI</returns>
        [HttpGet("heat-network/{hnId}")]
        [ProducesResponseType(typeof(List<AuditLogResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetHeatNetworkHistory(string hnId)
        {
            if (string.IsNullOrWhiteSpace(hnId))
            {
                return BadRequest("A valid Heat Network ID is required.");
            }

            // We pass <HeatNetwork> so the service targets "Audit_HeatNetworks"
            var history = await _auditService.GetAuditHistoryAsync<HeatNetwork>(hnId);

            if (history == null || !history.Any())
            {
                return NotFound(new { message = $"No audit history found for Heat Network: {hnId}" });
            }

            return Ok(history);
        }
    }
}
