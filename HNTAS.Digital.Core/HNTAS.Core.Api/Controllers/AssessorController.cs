using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models.Assessor;
using Microsoft.AspNetCore.Mvc;

namespace HNTAS.Core.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AssessorController : ControllerBase
    {
        private readonly IAssessorService _assessorService;

        public AssessorController(IAssessorService assessorService)
        {
            _assessorService = assessorService;
        }

        /// <summary>
        /// Search endpoint for the X-GOVUK Autocomplete component
        /// URL: /api/assessors/search?q=abc
        /// </summary>
        [HttpGet("search")]
        [ProducesResponseType(typeof(List<AssessorSearchResult>), 200)]
        public async Task<IActionResult> Search([FromQuery] string q)
        {
            // Validating the query length on the server side as well
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            {
                return Ok(new List<AssessorSearchResult>());
            }

            try
            {
                var results = await _assessorService.GetAssessorSuggestionsAsync(q);
                return Ok(results);
            }
            catch (Exception ex)
            {
                // Log the exception in a real app
                return StatusCode(500, "Internal server error during search");
            }
        }
    }
}
