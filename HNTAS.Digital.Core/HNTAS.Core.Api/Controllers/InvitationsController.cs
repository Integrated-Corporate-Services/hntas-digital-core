using AutoMapper;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models;
using HNTAS.Core.Api.Models.Users;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace HNTAS.Core.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvitationsController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IInvitationService _invitationService;
        private readonly ILogger<InvitationsController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly IHeatNetworkService _hnService;
        private readonly IMapper _mapper;


        public InvitationsController(
            IUserService userService,
            IInvitationService invitationService,
            ILogger<InvitationsController> logger,
            IConfiguration configuration,
            IEmailService emailService,
            IHeatNetworkService hnService,
            IMapper mapper)
        {
            _userService = userService;
            _invitationService = invitationService;
            _logger = logger;
            _configuration = configuration;
            _emailService = emailService;
            _hnService = hnService;
            _mapper = mapper;
        }

        /// <summary>
        /// Retrieves a specific invitation by its ID.
        /// </summary>
        /// <param name="id">The unique identifier of the invitation.</param>
        /// <returns>
        /// 200 OK with the invitation details if found;  
        /// 404 Not Found if no invitation exists with the given ID.
        /// </returns>
        [HttpGet("{id:length(24)}")]
        [ProducesResponseType(typeof(InvitedUserResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<InvitedUserResponse>> GetInvitationById(string id)
        {
            var invitation = await _invitationService.GetByIdAsync(id);
            if (invitation == null)
            {
                _logger.LogInformation("Invitation not found for the invitationId: {InvitationId}", id);
                return NotFound();
            }

            var response = _mapper.Map<InvitedUserResponse>(invitation);
            return Ok(response);
        }



        /// <summary>
        /// Updates New User Invitation in the User object
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <returns>204 when the update is successful</returns>
        [HttpPost("{id:length(24)}/add-user-invitation")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> AddUserInvitation(string id, [FromBody] AddInvitationRequest request)
        {

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid invitation data for user ID: {UserId}. Errors: {Errors}",
                    id, string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return ValidationProblem(ModelState);
            }

            try
            {

                //check if HnId exists in the system
                var hnExists = await _hnService.GetByHnIdAsync(request.HnId);
                if (hnExists == null)
                {
                    _logger.LogWarning("Heat Network with HnId {HnId} not found for invitation.", request.HnId);
                    return NotFound(new ProblemDetails
                    {
                        Status = StatusCodes.Status404NotFound,
                        Title = "Heat Network Not Found",
                        Detail = $"No heat network found with the provided HnId ({request.HnId})."
                    });
                }

                var existingUser = await _userService.GetByIdAsync(id);
                if (existingUser == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found for invitation update.", id);
                    return NotFound();
                }

                // Create a new Invitation document and save it to the new collection
                var newInvitation = new Invitation
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    InviterUserId = existingUser.Id, // Link to the user who sent the invite
                    InvitedEmail = request.EmailAddress,
                    InvitedHnId = request.HnId,
                    InvitedRoles = request.ContributorRoles,
                    Status = InvitationStatus.Invited, // Status should be 'Invited' for a new invitation
                    InvitedAt = DateTime.UtcNow
                };

                await _invitationService.CreateAsync(newInvitation); // Save the invitation to its collection

                _logger.LogInformation("Invitation sent by user {UserId}. New invitation ID: {InvitationId}", id, newInvitation.Id);

                return StatusCode(StatusCodes.Status201Created, newInvitation.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating an invitation for user with ID: {UserId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while creating the invitation.");
            }
        }

        /// <summary>
        /// Sends an invitation email for a specific invitation ID using the provided token.
        /// </summary>
        /// <param name="invitationId">The ID of the invitation to send.</param>
        /// <param name="token">The token used to personalize and secure the invitation link.</param>
        /// <returns>204 No Content if the email was sent successfully; 404 if the invitation or heat network is not found; 500 for unexpected errors.</returns>
        [HttpPost("{invitationId}/send-email")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendInvitationEmail(string invitationId, [FromBody] SendInvitationEmailRequest request)
        {
            var invitation = await _invitationService.GetByIdAsync(invitationId);

            if (invitation == null || invitation.Status != InvitationStatus.Invited)
            {
                _logger.LogInformation("Invitation not found for the invitationId : {InvitationId}", invitationId);
                return NotFound();
            }

            var hn = await _hnService.GetByHnIdAsync(invitation.InvitedHnId);

            await _emailService.TrySendInvitationEmailAsync(invitation, request.Token, hn.Name);

            _logger.LogInformation("Invitation email sent for ID: {InvitationId}", invitationId);

            return NoContent();
        }


        /// <summary>
        /// Rejects a pending invitation by ID.
        /// </summary>
        /// <param name="invitationId">The ID of the invitation to reject.</param>
        /// <returns>204 No Content if successful; 404 if not found; 400 if already accepted or rejected.</returns>
        [HttpPost("{invitationId}/Reject")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RejectInvitation(string invitationId)
        {
            var invitation = await _invitationService.GetByIdAsync(invitationId);
            if (invitation == null)
            {
                _logger.LogWarning("Invitation not found for ID: {InvitationId}", invitationId);
                return NotFound();
            }

            if (invitation.Status != InvitationStatus.Invited)
            {
                return BadRequest("Only pending invitations can be rejected.");
            }

            invitation.Status = InvitationStatus.Rejected;
            invitation.RejectedAt = DateTime.UtcNow;

            await _invitationService.UpdateAsync(invitationId, invitation);

            _logger.LogInformation("Invitation {InvitationId} was rejected.", invitationId);
            return NoContent();
        }

    }
}
