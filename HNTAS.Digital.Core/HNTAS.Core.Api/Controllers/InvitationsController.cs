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
        private readonly IOrganisationService _organisationService;
        private readonly INotificationHistoryService _notificationHistoryService;
        private readonly IMapper _mapper;


        public InvitationsController(
            IUserService userService,
            IInvitationService invitationService,
            ILogger<InvitationsController> logger,
            IConfiguration configuration,
            IEmailService emailService,
            IHeatNetworkService hnService,
            IMapper mapper,
            IOrganisationService organisationService,
            INotificationHistoryService notificationHistoryService)
        {
            _userService = userService;
            _invitationService = invitationService;
            _logger = logger;
            _configuration = configuration;
            _emailService = emailService;
            _hnService = hnService;
            _mapper = mapper;
            _organisationService = organisationService;
            _notificationHistoryService = notificationHistoryService;
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

                var hnDetails = null as HeatNetwork;
                //check if HnId exists in the system
                if (request.HnId != null)
                {
                    hnDetails = await _hnService.GetByHnIdAsync(request.HnId);
                    if (hnDetails == null)
                    {
                        _logger.LogWarning("Heat Network with HnId {HnId} not found for invitation.", request.HnId);
                        return NotFound(new ProblemDetails
                        {
                            Status = StatusCodes.Status404NotFound,
                            Title = "Heat Network Not Found",
                            Detail = $"No heat network found with the provided HnId ({request.HnId})."
                        });
                    }
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
                    InvitedOrgId = request.OrgId,
                    InvitedRoles = request.ContributorRoles,
                    Status = InvitationStatus.Invited, // Status should be 'Invited' for a new invitation
                    InvitedAt = DateTime.UtcNow,
                    RolesToReplace = request.RolesToReplace,
                    ReplacedUserId = request.ReplacedUserId
                };

                await _invitationService.CreateAsync(newInvitation); // Save the invitation to its collection

                _logger.LogInformation("Invitation sent by user {UserId}. New invitation ID: {InvitationId}", id, newInvitation.Id);


                if (request.ReplacedUserId != null && !(request.RolesToReplace.Contains(ContributorRole.ResponsiblePerson)
                    || request.RolesToReplace.Contains(ContributorRole.NetworkManager)))
                {
                    var userToUpdate = await _userService.GetByIdAsync(request.ReplacedUserId);
                    if (userToUpdate != null)
                    {
                        var rolesToRemove = request.RolesToReplace.ToHashSet();
                        userToUpdate.HnRoleMappings = userToUpdate.HnRoleMappings
                                                        .Where(mapping =>
                                                            mapping.HnId != request.HnId ||
                                                            !rolesToRemove.Contains(mapping.Role))
                                                        .ToList();

                        await _userService.UpdateAsync(userToUpdate.Id, userToUpdate);

                        //send an email to existing user that his heat network is discontinued
                        await _emailService.TrySendHNDiscontinedEmailAsync(userToUpdate, hnDetails?.Name, request.ContributorRoles.FirstOrDefault());
                    }
                }

                await NotificationHistoryForAddInvite(existingUser, newInvitation);

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

            if (invitation?.InvitedHnId != null)
            {
                var hn = await _hnService.GetByHnIdAsync(invitation.InvitedHnId);
                await _emailService.TrySendHeatNetworkInvitationEmailAsync(invitation, request.Token, hn?.Name!);
            }
            else if (invitation?.InvitedOrgId != null)
            {
                var inviterUser = await _userService.GetByIdAsync(invitation.InviterUserId);
                var userResponse = _mapper.Map<UserResponse>(inviterUser);
                var organisation = await _organisationService.GetByOrgIdAsync(invitation?.InvitedOrgId);
                await _emailService.TrySendOrganisationInvitationEmailAsync(invitation, request.Token, organisation.Name, userResponse?.FullName);
            }

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
            await NotificationHistoryForRejectInvite(invitation);
            _logger.LogInformation("Invitation {InvitationId} was rejected.", invitationId);
            return NoContent();
        }

        private async Task NotificationHistoryForAddInvite(User user, Invitation invitation)
        {
            var subject = string.Empty;
            var description = string.Empty;
            var inviterRole = user.Roles.FirstOrDefault();
            
            var date = DateTime.UtcNow;
            var action = string.Empty;
            var heatNetworkId = invitation.InvitedHnId;
            var eligibleRoles = new List<string>();
            NotificationHistoryType notificationType = NotificationHistoryType.NA;
            var invitedRole = invitation.InvitedRoles.FirstOrDefault();
            var invitedPerson = $"{invitation.FirstName} {invitation.LastName}".Trim();
            description = $"Email to {invitedPerson}";
            var actorIds = new List<string> { invitation.InviterUserId };
            if (inviterRole == UserRole.ResponsiblePerson)
            {                
                eligibleRoles = new List<string>
                {
                    ContributorRole.ResponsiblePerson.ToString(),                    
                };

                if (invitedRole == ContributorRole.DesignatedDesigner)
                {
                    subject = NotificationHistorySubjects.DesignatedDesignerInvited;
                    notificationType = NotificationHistoryType.RpInvitesDdhToHeatNetwork;
                }
                else if (invitedRole == ContributorRole.DesignatedContractor)
                {
                    subject = NotificationHistorySubjects.DesignatedContractorInvited;
                    notificationType = NotificationHistoryType.RpInvitesDdhToHeatNetwork;
                }
                else if (invitedRole == ContributorRole.DesignatedOperator)
                {
                    subject = NotificationHistorySubjects.DesignatedOperatorInvited;
                    notificationType = NotificationHistoryType.RpInvitesDdhToHeatNetwork;
                }
                else if (invitedRole == ContributorRole.NetworkManager)
                {
                    subject = NotificationHistorySubjects.NetworkManagerInvited;
                    notificationType = NotificationHistoryType.RpInvitesNetworkManager;
                }                
            }
            else if (inviterRole == UserRole.NetworkManager) // Network Manager
            {
                eligibleRoles = new List<string>
                {
                    ContributorRole.ResponsiblePerson.ToString(),
                    ContributorRole.NetworkManager.ToString()
                };
                notificationType = NotificationHistoryType.NetworkManagerInvitesDdhToHeatNetwork;
                if (invitedRole == ContributorRole.DesignatedDesigner)
                {
                    subject = NotificationHistorySubjects.DesignatedDesignerInvited;                    
                }
                else if (invitedRole == ContributorRole.DesignatedContractor)
                {
                    subject = NotificationHistorySubjects.DesignatedContractorInvited;                    
                }
                else if (invitedRole == ContributorRole.DesignatedOperator)
                {
                    subject = NotificationHistorySubjects.DesignatedOperatorInvited;                    
                }                
            }
            else if (inviterRole == UserRole.DesignatedDutyHolder)
            {
                await AddDdhInvitorToActorIds(invitation, actorIds);

                eligibleRoles = new List<string>
                {
                    ContributorRole.ResponsiblePerson.ToString(),
                    ContributorRole.NetworkManager.ToString(),                    
                };
                notificationType = NotificationHistoryType.DdhInvitesContributorToHeatNetwork;

                if (invitedRole == ContributorRole.ContributingDesigner)
                {
                    eligibleRoles.Add(ContributorRole.DesignatedDesigner.ToString());
                    subject = NotificationHistorySubjects.ContributingDesignerInvited;                    
                }
                else if (invitedRole == ContributorRole.ContributingContractor)
                {
                    eligibleRoles.Add(ContributorRole.DesignatedContractor.ToString());
                    subject = NotificationHistorySubjects.ContributingContractorInvited;                    
                }
                else if (invitedRole == ContributorRole.ContributingOperator)
                {
                    eligibleRoles.Add(ContributorRole.DesignatedOperator.ToString());
                    subject = NotificationHistorySubjects.ContributingOperatorInvited;                    
                }
            }            

            var notificationHistory = new NotificationHistory
            {
                NotificationType = notificationType,
                ActorsId = actorIds,
                Subject = subject,
                Description = description,
                Timestamp = date,
                Action = action,
                HeatNetworkId = heatNetworkId,
                CreatedBy = invitation.InviterUserId,
                EligibleRoles = eligibleRoles
            };
            await _notificationHistoryService.CreateAsync(notificationHistory);
        }

        private async Task NotificationHistoryForRejectInvite(Invitation invitation)
        {
            var subject = string.Empty;
            var description = string.Empty;
            var date = DateTime.UtcNow;
            var action = string.Empty;            
            var heatNetworkId = invitation.InvitedHnId;
            var eligibleRoles = new List<string> { ContributorRole.ResponsiblePerson.ToString() };
            NotificationHistoryType notificationType = NotificationHistoryType.NA;
            var invitedRole = invitation.InvitedRoles.FirstOrDefault();
            var invitedPerson = $"{invitation.FirstName} {invitation.LastName}".Trim();
            description = $"{invitation.FirstName} {invitation.LastName} rejected, please take alternate action";
            var actorIds = new List<string>() { invitation.InviterUserId };

            if (invitedRole == ContributorRole.DesignatedDesigner)
            {
                eligibleRoles.Add(ContributorRole.NetworkManager.ToString());
                subject = NotificationHistorySubjects.DesignatedDesignerRejected;
                notificationType = NotificationHistoryType.DdhRejectsInviteToHeatNetwork;
                action = NotificationHistoryActions.DDHAndContributors;
            }
            else if (invitedRole == ContributorRole.DesignatedContractor)
            {
                eligibleRoles.Add(ContributorRole.NetworkManager.ToString());
                subject = NotificationHistorySubjects.DesignatedContractorRejected;
                notificationType = NotificationHistoryType.DdhRejectsInviteToHeatNetwork;
                action = NotificationHistoryActions.DDHAndContributors;
            }
            else if (invitedRole == ContributorRole.DesignatedOperator)
            {
                eligibleRoles.Add(ContributorRole.NetworkManager.ToString());
                subject = NotificationHistorySubjects.DesignatedOperatorRejected;
                notificationType = NotificationHistoryType.DdhRejectsInviteToHeatNetwork;
                action = NotificationHistoryActions.DDHAndContributors;
            }
            else if (invitedRole == ContributorRole.ContributingDesigner)
            {
                await AddDdhInvitorToActorIds(invitation, actorIds);
                eligibleRoles.Add(ContributorRole.NetworkManager.ToString());
                subject = NotificationHistorySubjects.ContributingDesignerRejected;
                notificationType = NotificationHistoryType.ContributorRejectsInviteToHeatNetwork;
                action = NotificationHistoryActions.DDHAndContributors;
            }
            else if (invitedRole == ContributorRole.ContributingContractor)
            {
                await AddDdhInvitorToActorIds(invitation, actorIds);
                eligibleRoles.Add(ContributorRole.NetworkManager.ToString());
                subject = NotificationHistorySubjects.ContributingContractorRejected;
                notificationType = NotificationHistoryType.ContributorRejectsInviteToHeatNetwork;
                action = NotificationHistoryActions.DDHAndContributors;
            }
            else if (invitedRole == ContributorRole.ContributingOperator)
            {
                await AddDdhInvitorToActorIds(invitation, actorIds);
                eligibleRoles.Add(ContributorRole.NetworkManager.ToString());
                subject = NotificationHistorySubjects.ContributingOperatorRejected;
                notificationType = NotificationHistoryType.ContributorRejectsInviteToHeatNetwork;
                action = NotificationHistoryActions.DDHAndContributors;
            }
            else if (invitedRole == ContributorRole.NetworkManager)
            {
                subject = NotificationHistorySubjects.NetworkManagerRejected;
                notificationType = NotificationHistoryType.NetworkManagerRejectsInvite;
                action = NotificationHistoryActions.NetworkManagers;
            }            

            var notificationHistory = new NotificationHistory
            {
                NotificationType = notificationType,
                ActorsId = actorIds,
                Subject = subject,
                Description = description,
                Timestamp = date,
                Action = action,
                HeatNetworkId = heatNetworkId,
                CreatedBy = invitation.InviterUserId,
                EligibleRoles = eligibleRoles
            };
            await _notificationHistoryService.CreateAsync(notificationHistory);
        }

        private async Task AddDdhInvitorToActorIds(Invitation invitation, List<string> actorIds)
        {
            var invitorDetailsOfDdh = await _userService.GetByIdAsync(invitation.InviterUserId);
            if (invitorDetailsOfDdh == null) return;

            var role = invitorDetailsOfDdh.HnRoleMappings
                .Where(mapping => mapping.HnId == invitation.InvitedHnId)
                .Select(mapping => mapping.Role)
                .FirstOrDefault();

            var invitorOfDdh = await _invitationService.GetByInvitedDetailsAsync(
                invitorDetailsOfDdh.EmailId,
                invitation.InvitedHnId!,
                role);

            if (invitorOfDdh != null)
                actorIds.Add(invitorOfDdh.InviterUserId!);
        }
    }
}