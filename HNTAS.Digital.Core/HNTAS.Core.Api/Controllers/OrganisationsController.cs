using AutoMapper;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models.Users;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace HNTAS.Core.Api.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class OrganisationsController : ControllerBase
    {
        private readonly IOrganisationService _organizationService;
        private readonly IUserService _userService;
        private readonly IEmailService _emailService;
        private readonly IOrganisationService _organisationService;
        private readonly ILogger<OrganisationsController> _logger;
        private readonly IMapper _mapper;

        public OrganisationsController(
            IOrganisationService organizationService,
            IUserService userService,
            IEmailService emailService,
            ILogger<OrganisationsController> logger,
            IMapper mapper)
        {
            _logger = logger;
            _mapper = mapper;
            _organizationService = organizationService;
            _userService = userService;
            _emailService = emailService;
        }

        [HttpPatch("{orgId}/edit-org-details")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> EditOrgDetails(string orgId, string userId, [FromBody] OrganisationRequest request)
        {

            try
            {
                var existingUser = await _userService.GetByIdAsync(userId);
                if (existingUser == null)
                {
                    _logger.LogWarning("User with ID: {userId} not found for update.", userId);
                    return NotFound($"User with ID: {userId} not found.");
                }
                var existingOrg = await _organizationService.GetByOrgIdAsync(orgId);
                if (existingOrg == null)
                {
                    _logger.LogWarning("Organisation with ID: {orgId} not found for update.", orgId);
                    return NotFound($"Organisation with ID: {orgId} not found.");
                }
                RegisteredAddress oldAddress = existingOrg.RegisteredAddress;
                string fullName = existingUser.FirstName + " " + existingUser.LastName;

                // Create a new Organization document using the data from the request
                var updateOrg = new Organisation
                {
                    Type = request.Type,
                    CompaniesHouseNumber = request.CompaniesHouseNumber,
                    Name = request.Name,
                    RegisteredAddress = _mapper.Map<RegisteredAddress>(request.RegisteredAddress)
                };

                await _organizationService.UpdateAsync(existingOrg.Id, updateOrg); // Save the new organization to its collection

                _emailService.TrySendOrgUpdatedEmailAsync(fullName, existingUser.EmailId, oldAddress, updateOrg.RegisteredAddress);

                return NoContent();

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error updating organisation details: {ex.Message}");
            }
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