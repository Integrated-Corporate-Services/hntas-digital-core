using AutoMapper;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Helpers;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models.Users;
using HNTAS.Core.Api.Services;
using Microsoft.AspNetCore.Http;
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
        private readonly ILogger<UsersController> _logger;
        private readonly IMapper _mapper;

        public OrganisationsController(
            IOrganisationService organizationService,
            IUserService userService,
            IEmailService emailService,
            ILogger<UsersController> logger,
            IMapper mapper)
        {
            _organizationService = organizationService;
            _userService = userService;
            _emailService = emailService;
            _logger = logger;
            _mapper = mapper;
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
                if (existingUser == null) {
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
    }
}
