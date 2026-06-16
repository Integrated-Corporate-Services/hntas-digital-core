using AutoMapper;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models.Users;
using Microsoft.AspNetCore.Mvc;

namespace HNTAS.Core.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrganisationUserController : ControllerBase
    {
        private readonly IOrganisationService _orgService;
        private readonly IUserService _userService;
        private readonly IMapper _mapper;
        private readonly ILogger<OrganisationUserController> _logger;
        public OrganisationUserController(IOrganisationService orgService, IUserService userService, IMapper mapper, ILogger<OrganisationUserController> logger)
        {
            _orgService = orgService;
            _userService = userService;
            _mapper = mapper;
            _logger = logger;
        }


        /// <summary>
        /// Retrieves the details of the Responsible Party (RP) user for a given Organisation ID.
        /// GET /api/OrganisationUser/responsible-party/{orgId}
        /// </summary>
        /// <param name="orgId">The unique OrgId of the organisation.</param>
        [HttpGet("responsible-party-user/{orgId}")]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<UserResponse>> GetResponsiblePartyDetails(string orgId)
        {
            if (string.IsNullOrWhiteSpace(orgId))
            {
                return BadRequest("OrgId must be provided.");
            }

            var organisation = await _orgService.GetByOrgIdAsync(orgId);

            if (organisation == null)
            {
                return NotFound($"Organisation with OrgId '{orgId}' not found.");
            }

            string rpUserId = organisation.RpUserId;

            if (string.IsNullOrWhiteSpace(rpUserId))
            {
                return NotFound($"No Responsible Party (RP) is assigned to Organisation '{orgId}'.");
            }

            var rpUserDetails = await _userService.GetByIdAsync(rpUserId);

            if (rpUserDetails == null)
            {
                return StatusCode(500, $"Internal Error: RP User ID '{rpUserId}' found but user details could not be retrieved.");
            }

            var userResponse = _mapper.Map<UserResponse>(rpUserDetails);

            return Ok(userResponse);
        }
    }
}
