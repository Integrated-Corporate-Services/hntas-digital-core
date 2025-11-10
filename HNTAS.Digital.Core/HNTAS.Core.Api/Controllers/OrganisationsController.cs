using AutoMapper;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Helpers;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models.Users;
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
        private readonly ILogger<UsersController> _logger;
        private readonly IMapper _mapper;

        public OrganisationsController(
            IOrganisationService organizationService,
            ILogger<UsersController> logger,
            IMapper mapper)
        {
            _organizationService = organizationService;
            _logger = logger;
            _mapper = mapper;
        }

        [HttpPatch("{orgId:length(10)}/edit-org-details")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<User>> EditOrgDetails(string orgId, [FromBody] OrganisationRequest request)
        {
            
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid organisation details update data for orgnisation ID: {orgId}. Errors: {Errors}",
                    orgId, string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return ValidationProblem(ModelState);
            }

            try
            {
                var existingOrg = await _organizationService.GetByIdAsync(orgId);
                if (existingOrg == null)
                {
                    _logger.LogWarning("Organisation with ID: {orgId} not found for update.", orgId);
                    return NotFound($"Organisation with ID: {orgId} not found.");
                }

                // Create a new Organization document using the data from the request
                var updateOrg = new Organisation
                {
                    Type = request.Type,
                    CompaniesHouseNumber = request.CompaniesHouseNumber,
                    Name = request.Name,
                    RegisteredAddress = _mapper.Map<RegisteredAddress>(request.RegisteredAddress)
                };

                await _organizationService.UpdateAsync(orgId, updateOrg); // Save the new organization to its collection

                // send email here

                return NoContent();

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error updating organisation details: {ex.Message}");
            }
        }
    }
}
