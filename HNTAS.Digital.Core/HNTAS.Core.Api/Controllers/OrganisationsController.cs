using HNTAS.Core.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HNTAS.Core.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrganisationsController : ControllerBase
    {
        private readonly IOrganisationService _organisationService;
        private readonly ILogger<OrganisationsController> _logger;


        public OrganisationsController(IOrganisationService organisationService, ILogger<OrganisationsController> logger)
        {
            _organisationService = organisationService;
            _logger = logger;
        }

        /// <summary>
        /// Checks if an organization exists based on its name, postcode, and country.
        /// </summary>
        /// <param name="name">The organization's name.</param>
        /// <param name="postCode">The organization's postcode.</param>
        /// <param name="country">The organization's country.</param>
        /// <returns>A status code indicating existence (200 OK) or non-existence (404 Not Found).</returns>
        [HttpGet("exists-by-details")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<bool>> ExistsByDetails(
            [FromQuery] string name,
            [FromQuery] string postCode,
            [FromQuery] string country)
        {
            // Input validation (optional, but recommended)
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(postCode) || string.IsNullOrEmpty(country))
            {
                return BadRequest("Name, postcode, and country are required parameters.");
            }

            // Call the repository method
            bool exists = await _organisationService.ExistsByDetailsAsync(name, postCode, country);

            return Ok(exists);
        }
    }
}
