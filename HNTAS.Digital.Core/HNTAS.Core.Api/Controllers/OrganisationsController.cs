using AutoMapper;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Helpers;
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
        private readonly IUserService _userService;
        private readonly IEmailService _emailService;
        private readonly IOrganisationService _organisationService;
        private readonly ILogger<OrganisationsController> _logger;
        private readonly IMapper _mapper;

        public OrganisationsController(
            IOrganisationService organisationService,
            IUserService userService,
            IEmailService emailService,
            ILogger<OrganisationsController> logger,
            IMapper mapper)
        {
            _logger = logger;
            _mapper = mapper;
            _organisationService = organisationService;
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
                var existingOrg = await _organisationService.GetByOrgIdAsync(orgId);
                if (existingOrg == null)
                {
                    _logger.LogWarning("Organisation with ID: {orgId} not found for update.", orgId);
                    return NotFound($"Organisation with ID: {orgId} not found.");
                }

                string fullName = StringFormatter.ToTitleCaseSingleWord(existingUser.FirstName) + " " + StringFormatter.ToTitleCaseSingleWord(existingUser.LastName);

                // update Organization document using the data from the request

                var oldNameAndAddress = $"{existingOrg.Name}, {StringFormatter.FormatAddress(existingOrg.RegisteredAddress)}";
                var newNameAndAddress = $"{request.Name}, {StringFormatter.FormatAddress(request.RegisteredAddress)}";


                existingOrg.Type = request.Type;
                existingOrg.CompaniesHouseNumber = request.CompaniesHouseNumber;
                existingOrg.Name = request.Name;
                existingOrg.RegisteredAddress = request.RegisteredAddress;
                existingOrg.LastModifiedAt = DateTime.UtcNow;
                existingOrg.LastModifiedBy = userId;


                await _organisationService.UpdateAsync(existingOrg.Id, existingOrg);

                _emailService.TrySendOrgUpdatedEmailAsync(fullName, existingUser.EmailId, oldNameAndAddress, newNameAndAddress);

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


        /// <summary>
        /// Searches for an organisation by its unique OrgId or by its Name.
        /// </summary>
        /// <param name="term">The OrgId or Name to search for.</param>
        [HttpGet("search")] // The full route will be 'GET /api/organisations/search?term=VALUE'
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Organisation))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Organisation>> GetByOrgIdOrName([FromQuery] string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return BadRequest("Search term cannot be empty.");
            }

            var organisation = await _organisationService.GetByOrgIdOrNameAsync(term);

            if (organisation == null)
            {
                return NotFound($"No organisation found for search term: '{term}'");
            }

            return Ok(organisation);
        }

        /// <summary>
        /// Retrieves a single organisation by its unique OrgId.
        /// </summary>
        /// <param name="orgId">The unique OrgId to search for.</param>
        [HttpGet("{orgId}")] // The full route will be 'GET /api/organisations/{orgId}'
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Organisation))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Organisation>> GetByOrgId([FromRoute] string orgId)
        {
            if (string.IsNullOrWhiteSpace(orgId))
            {
                return BadRequest("Organisation ID cannot be empty.");
            }

            // Call the dedicated service method
            var organisation = await _organisationService.GetByOrgIdAsync(orgId);

            if (organisation == null)
            {
                return NotFound($"Organisation with ID '{orgId}' not found.");
            }

            return Ok(organisation);
        }



        [HttpPatch("{orgId}/user/{userId}/heatnetwork/{heatNetworkId}")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> UpdateHeatNetworkId([FromRoute] string orgId, [FromRoute] string userId, [FromRoute] string heatNetworkId)
        {
            try
            {
                var organisation = await _organisationService.GetByOrgIdAsync(orgId.ToUpper());

                if (organisation == null)
                {
                    _logger.LogWarning("Organisation with OrgId {OrgId} not found for heat network ID update.", orgId);
                    return NotFound();
                }

                if (!organisation.HnIds.Contains(heatNetworkId))
                {
                    organisation.HnIds.Add(heatNetworkId);
                    organisation.LastModifiedAt = DateTime.UtcNow;
                    organisation.LastModifiedBy = userId;
                    await _organisationService.UpdateAsync(organisation.Id, organisation);
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating heat network ID for organisation with OrgId: {OrgId}", orgId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while updating the heat network ID.");
            }
        }
    }

}