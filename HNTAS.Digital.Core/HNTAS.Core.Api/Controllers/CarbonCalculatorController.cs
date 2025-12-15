using HNTAS.Core.Api.Models;
using HNTAS.Core.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace HNTAS.Core.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class CarbonCalculatorController : ControllerBase
    {
        private readonly ICarbonCalculatorService _service;

        public CarbonCalculatorController(ICarbonCalculatorService service)
        {
            _service = service;
        }

        [HttpPost("run")]
        public async Task<ActionResult<CarbonCalculatorResponse>> RunAsync(CarbonCalculatorRequest request, CancellationToken ct = default)
        {
            var result = await _service.RunAsync(request , ct);
            if (result is null)
                return Problem("Calculation failed or API token missing.");

            return Ok(result);
        }
    }
}