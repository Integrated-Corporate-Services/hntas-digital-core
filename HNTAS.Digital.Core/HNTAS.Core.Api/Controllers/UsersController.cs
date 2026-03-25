using AutoMapper;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Helpers;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models;
using HNTAS.Core.Api.Models.Users;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System.Net.Mime;

namespace HNTAS.Core.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IOrganisationService _organisationService;
    private readonly IInvitationService _invitationService;
    private readonly ILogger<UsersController> _logger;
    private readonly ICounterService _orgCounterService;
    private readonly IMapper _mapper;
    private readonly IEmailService _emailService;
    private readonly IHeatNetworkService _heatNetworkService;
    private readonly IAuditService _auditService;


    public UsersController(IUserService userService,
                           IOrganisationService organizationService,
                           IInvitationService invitationService,
                           ILogger<UsersController> logger,
                           ICounterService orgCounterService,
                           IMapper mapper,
                           IEmailService emailService,
                           IHeatNetworkService heatNetworkService,
                           IAuditService auditService)
    {
        _userService = userService;
        _organisationService = organizationService;
        _invitationService = invitationService;
        _logger = logger;
        _emailService = emailService;
        _orgCounterService = orgCounterService;
        _mapper = mapper;
        _heatNetworkService = heatNetworkService;
        _auditService = auditService;
    }

    /// <summary>
    /// Retrieves a list of all users.
    /// </summary>
    /// <returns>A list of user objects.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<UserResponse>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<UserResponse>>> GetUsers()
    {
        _logger.LogInformation("Attempting to retrieve all users.");
        try
        {
            var users = await _userService.GetAsync();
            var userResponseList = _mapper.Map<List<UserResponse>>(users);
            _logger.LogInformation("Successfully retrieved {UserCount} users.", users.Count);
            return Ok(userResponseList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving all users.");
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while retrieving users.");
        }
    }

    /// <summary>
    /// Retrieves a list of all users.
    /// </summary>
    /// <returns>A list of user objects.</returns>
    [HttpGet("user-details-by-id")]
    [ProducesResponseType(typeof(UserDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserDetailsResponse))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserDetailsResponse>> GetUsersDetails(string id)
    {
        _logger.LogInformation("Attempting to retrieve all users.");
        try
        {
            var userDetails = await _userService.GetUserWithDetailsAsync(id);

            var userResponse = _mapper.Map<UserDetailsResponse>(userDetails);

            //Manual mapping needed because of the complexity
            userResponse.HeatNetworks = GetHeatNetworksForUser(userDetails);

            _logger.LogInformation("Successfully retrieved {UserCount} users.", userResponse?.Id);
            return Ok(userResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving all users.");
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while retrieving users.");
        }
    }


    // <summary>
    /// Get a User by their ID
    /// </summary>
    /// <remarks>
    /// Retrieves a single user profile from the database using their unique ID.
    /// This endpoint is used to fetch the complete details of an existing user.
    /// </remarks>
    /// <param name="id">The unique ID (24-character hexadecimal string) of the user to retrieve.</param>
    /// <returns>
    /// A <see cref="StatusCodes.Status200OK"/> (OK) response with the found user object,
    /// or a <see cref="StatusCodes.Status404NotFound"/> (Not Found) response if no user matches the provided ID.
    /// </returns>
    [HttpGet("{id:length(24)}", Name = "GetUserById")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserResponse>> GetById(string id)
    {
        _logger.LogInformation("Attempting to retrieve user with ID: {Id}", id);
        try
        {
            var user = await _userService.GetByIdAsync(id);

            if (user == null)
            {
                _logger.LogWarning("User with ID {Id} not found.", id);
                return NotFound();
            }
            _logger.LogInformation("Successfully retrieved user with ID: {Id}", id);
            var userResponse = _mapper.Map<UserResponse>(user);
            return Ok(userResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating user ID format: {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while validating the user ID.");
        }
    }


    /// <summary>
    /// Check if a user is a Responsible Person by their email ID
    /// </summary>
    /// <remarks>
    /// Validates whether the user associated with the given email ID has the RegulatoryContact role.
    /// </remarks>
    /// <param name="emailId">The email ID of the user to check.</param>
    /// <returns>
    /// A <see cref="StatusCodes.Status200OK"/> response with a boolean indicating role membership,
    /// or a <see cref="StatusCodes.Status404NotFound"/> if the user is not found.
    /// </returns>
    [HttpGet("is-rp-user/{emailId}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<bool>> IsRpUser(string emailId)
    {
        var sanitizedEmailId = emailId?.Replace("\r", "").Replace("\n", "");

        _logger.LogInformation("Checking if user with email ID {EmailId} is a Responsible Person.", sanitizedEmailId);
        try
        {
            var user = await _userService.GetByEmailAsync(emailId);

            if (user == null)
            {
                _logger.LogWarning("User with email ID {EmailId} not found.", sanitizedEmailId);
                return NotFound();
            }

            bool isRegulatoryContact = user.Roles.Contains(UserRole.ResponsiblePerson);
            _logger.LogInformation("User with email ID {EmailId} is Responsible Person: {IsRp}", sanitizedEmailId, isRegulatoryContact);
            return Ok(isRegulatoryContact);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while checking Responsible Person role for email ID: {EmailId}", sanitizedEmailId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while checking the user's role.");
        }
    }


    [HttpGet("is-active-user/{emailId}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(bool), StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<ActionResult<bool>> IsActiveUser(string emailId)
    {
        if (string.IsNullOrWhiteSpace(emailId))
        {
            return BadRequest("Email ID must be provided.");
        }

        var user = await _userService.GetByEmailAsync(emailId);

        if (user == null)
        {
            // User not found, so not active
            return NotFound();
        }

        // Assuming user.Status is an enum or property indicating active state
        bool isActive = user.Status == UserStatus.Active;

        return Ok(isActive);
    }

    /// <summary>
    /// Get a User by their OneLogin ID
    /// </summary>
    /// <param name="oneLoginId"></param>
    /// <returns></returns>
    [HttpGet("onelogin/{oneLoginId}", Name = "GetUserByOneLoginId")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserResponse>> GetUserByOneLoginId(string oneLoginId)
    {
        _logger.LogInformation("Attempting to retrieve user with ID: {Id}", oneLoginId);

        try
        {
            var user = await _userService.GetByUserOneLoginIdAsync(oneLoginId);

            if (user == null)
            {
                _logger.LogWarning("User with ID {Id} not found.", oneLoginId);
                return NotFound();
            }

            _logger.LogInformation("Successfully retrieved user with ID: {Id}", oneLoginId);
            var userResponse = _mapper.Map<UserResponse>(user);
            return Ok(userResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by OneLogin ID: {Id}", oneLoginId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while retrieving the user.");
        }
    }


    /// <summary>
    /// Register initial user after login
    /// </summary>
    /// <remarks>Creates a new user entry with minimal details upon first login, setting status to pending org setup.</remarks>
    /// <param name="registrationData">The initial user registration data (UserId, EmailId).</param>
    /// <returns>A newly created user profile or an existing one if already registered.</returns>
    [HttpPost("initial-entry")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<string>> InitialRegisterUser([FromBody] InitialUserRegistrationRequest registrationData)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid initial registration data for UserId: {UserId}, EmailId: {EmailId}. Errors: {Errors}",
                registrationData.OneLoginId, registrationData.EmailId, string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            return ValidationProblem(ModelState);
        }

        try
        {
            var existingUser = await _userService.GetByUserOneLoginIdAsync(registrationData.OneLoginId);

            if (existingUser != null)
            {
                return Conflict(new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "User Already Exists",
                    Detail = $"A user with the provided UserId ({registrationData.OneLoginId}) already exists."
                });
            }

            var newUser = new User
            {
                OneLoginId = registrationData.OneLoginId,
                EmailId = registrationData.EmailId,
                Status = registrationData.Status,
                CreatedAt = DateTime.UtcNow,
            };

            await _userService.CreateAsync(newUser);

            _logger.LogInformation("New user initially registered: {UserId} (DB Id: {Id})", newUser.OneLoginId, newUser.Id);

            return StatusCode(StatusCodes.Status201Created, newUser.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during initial user registration for UserId: {UserId}, EmailId: {EmailId}", registrationData.OneLoginId, registrationData.EmailId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred during initial user registration."
            });
        }
    }

    /// <summary>
    /// Update Organisation Details for a User
    /// </summary>
    [HttpPatch("{id:length(24)}/org-details")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<User>> UpdateUserAndOrgDetails(string id, [FromBody] UpdateUserOrganisationRequest request)
    {
        // Contact details validation logic remains the same
        var (landline, extension, mobile) = ContactDetailsValidationHelper.GetValidatedContactDetails(
            request.PreferredContactType,
            request.LandlineNumber,
            request.ContactNumberExtension,
            request.MobileNumber,
            ModelState
        );

        request.LandlineNumber = landline;
        request.ContactNumberExtension = extension;
        request.MobileNumber = mobile;

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid organisation details update data for user ID: {UserId}. Errors: {Errors}",
                id, string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            return ValidationProblem(ModelState);
        }

        try
        {
            var existingUser = await _userService.GetByIdAsync(id);
            if (existingUser == null)
            {
                _logger.LogWarning("User with ID {UserId} not found for organisation details update.", id);
                return NotFound();
            }

            // Create a new Organization document using the data from the request
            var newOrg = new Organisation
            {
                OrgId = $"ORG{await _orgCounterService.GetNextSequenceValue("orgId_sequence"):D7}",
                Type = request.Organisation.Type,
                CompaniesHouseNumber = request.Organisation.CompaniesHouseNumber,
                Name = request.Organisation.Name,
                RegisteredAddress = _mapper.Map<RegisteredAddress>(request.Organisation.RegisteredAddress),
                CreatedBy = existingUser.Id,
                CreatedAt = DateTime.UtcNow,
                RpUserId = existingUser.Id
            };

            await _organisationService.CreateAsync(newOrg); // Save the new organization to its collection

            // Update the existing User document to link to the new Organization
            existingUser.OrgId = newOrg.OrgId;
            existingUser.FirstName = request.FirstName;
            existingUser.LastName = request.LastName;
            existingUser.JobTitle = request.JobTitle;
            existingUser.PreferredContactType = request.PreferredContactType;
            existingUser.LandlineNumber = request.LandlineNumber;
            existingUser.MobileNumber = request.MobileNumber;
            existingUser.ContactNumberExtension = request.ContactNumberExtension;
            existingUser.Status = UserStatus.Active; // Set status as active here

            if (existingUser.Roles == null)
            {
                existingUser.Roles = new List<UserRole>() { request.Role };
            }
            else if (!existingUser.Roles.Contains(request.Role))
            {
                existingUser.Roles.Add(request.Role);
            }

            await _userService.UpdateAsync(id, existingUser);

            _logger.LogInformation("Organisation details and status updated for user {UserId}. Generated OrgId: {OrgId}", id, newOrg.Id);

            await _emailService.TrySendOrgCreatedEmailAsync(existingUser, newOrg);

            return Ok(existingUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating organisation details for user {UserId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while updating Organisation details."
            });
        }
    }


    /// <summary>
    /// Registers a new Organisation and links its generated OrgId to the specified User.
    /// </summary>
    /// <param name="userId">The ID of the user whose OrgId field will be updated.</param>
    /// <param name="request">The data to create the new Organisation record.</param>
    /// <returns>The newly created Organisation object.</returns>
    [HttpPost("register-org-and-link/{userId}")]
    [ProducesResponseType(typeof(Organisation), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Organisation>> RegisterOrganisationAndLinkUserAsync(
        string userId,
        OrganisationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (string.IsNullOrWhiteSpace(userId) || !ObjectId.TryParse(userId, out _))
        {
            return BadRequest("Invalid or missing UserId.");
        }

        try
        {
            // Check the user exists
            var existingUser = await _userService.GetByIdAsync(userId);
            if (existingUser == null)
            {
                _logger.LogWarning("User with ID '{UserId}' not found for organisation registration.", userId);
                return NotFound($"User with ID '{userId}' was not found. Organisation was not created.");
            }

            var newOrganisation = new Organisation
            {
                OrgId = $"ORG{await _orgCounterService.GetNextSequenceValue("orgId_sequence"):D7}",
                Name = request.Name,
                CompaniesHouseNumber = request.CompaniesHouseNumber,
                Type = request.Type,
                RegisteredAddress = _mapper.Map<RegisteredAddress>(request.RegisteredAddress),
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
            };

            await _organisationService.CreateAsync(newOrganisation);

            _logger.LogInformation("Organisation created successfully. New OrgId: {OrgId}", newOrganisation.OrgId);

            var updateResult = await _userService.UpdateOrgIdAsync(userId, newOrganisation.OrgId);

            if (updateResult.ModifiedCount == 0)
            {
                _logger.LogError("Failed to modify user {UserId} OrgId. Matched: {Matched}, Modified: {Modified}. Starting rollback.", userId, updateResult.MatchedCount, updateResult.ModifiedCount);

                // Initiate Rollback: Delete the newly created Organisation
                await _organisationService.RemoveAsync(newOrganisation.Id);

                // Return a Server Error indicating the linking failed
                return StatusCode(StatusCodes.Status500InternalServerError, $"Organisation created but failed to link to user {userId}. Rollback executed.");
            }

            _logger.LogInformation("User {UserId} successfully updated with OrgId: {OrgId}. Modified count: {Count}",
                userId, newOrganisation.OrgId, updateResult.ModifiedCount);

            // Return the created organisation object with 201 status
            return StatusCode(StatusCodes.Status201Created, newOrganisation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create new organisation record.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                "An unexpected error occurred during registration and linking: " + ex.Message);
        }
    }

    /// <summary>
    /// Update User Details
    /// </summary>
    [HttpPatch("{id:length(24)}/user-details")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<User>> UpdateUserDetails(string id, [FromBody] UpdateUserDetailsRequest request)
    {
        // 1. Contact details validation logic remains the same
        var (landline, extension, mobile) = ContactDetailsValidationHelper.GetValidatedContactDetails(
            request.PreferredContactType,
            request.LandlineNumber,
            request.ContactNumberExtension,
            request.MobileNumber,
            ModelState
        );

        request.LandlineNumber = landline;
        request.ContactNumberExtension = extension;
        request.MobileNumber = mobile;

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid user details update data for user ID: {UserId}. Errors: {Errors}",
                id, string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            return ValidationProblem(ModelState);
        }

        try
        {
            // 2. Find the existing user
            var existingUser = await _userService.GetByIdAsync(id);
            if (existingUser == null)
            {
                _logger.LogWarning("User with ID {UserId} not found for user details update.", id);
                return NotFound();
            }

            // 3. Update only the user-specific fields
            existingUser.FirstName = request.FirstName;
            existingUser.LastName = request.LastName;
            existingUser.JobTitle = request.JobTitle;
            existingUser.PreferredContactType = request.PreferredContactType;
            existingUser.LandlineNumber = request.LandlineNumber;
            existingUser.MobileNumber = request.MobileNumber;
            existingUser.ContactNumberExtension = request.ContactNumberExtension;


            if (request.Role != null)
            {
                if (existingUser.Roles == null)
                {
                    existingUser.Roles = new List<UserRole>() { request.Role.Value };
                }
                else if (!existingUser.Roles.Contains(request.Role.Value))
                {
                    existingUser.Roles.Add(request.Role.Value);
                }
            }

            await _userService.UpdateAsync(id, existingUser);

            _logger.LogInformation("User details updated for user {UserId}.", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user details for user {UserId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while updating User details."
            });
        }
    }

    [HttpPatch("accept-invitation")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> AcceptInvitationAsync(InvitedUserRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));

            _logger.LogWarning("Invalid invited user data. Errors: {Errors}", errors);
            return ValidationProblem(ModelState);
        }

        try
        {
            // Retrieve invitation
            var invitation = await _invitationService.GetByIdAsync(request.InvitationId);
            if (invitation == null)
            {
                _logger.LogWarning("Invitation not found for email: {Email}", request.InvitedEmail);
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Invitation Not Found",
                    Detail = $"No invitation found for email ({request.InvitedEmail})."
                });
            }            

            // Mark invitation as accepted
            invitation.Status = InvitationStatus.Accepted;
            invitation.AcceptedAt = DateTime.UtcNow;

            // Check for existing user
            var invitedUser = await _userService.GetByUserOneLoginIdAsync(request.OneLoginId);

            if (invitedUser != null && invitation.InvitedHnId != null)
            {
                invitedUser.HnRoleMappings = invitedUser.HnRoleMappings ?? new List<HnRoleMapping>();

                if (invitation.InvitedOrgId != null)
                {
                    invitedUser.OrgId = invitation.InvitedOrgId;
                }

                foreach (var role in invitation.InvitedRoles)
                {
                    // Update HnRoleMappings
                    var hnRoleMapping = invitedUser.HnRoleMappings.FirstOrDefault(m => m.HnId == invitation.InvitedHnId);
                    if (hnRoleMapping == null)
                    {
                        hnRoleMapping = new HnRoleMapping
                        {
                            HnId = invitation.InvitedHnId,
                            Role = role,
                        };
                        invitedUser.HnRoleMappings.Add(hnRoleMapping);
                    }
                    else if (!(hnRoleMapping.Role == role))
                    {
                        hnRoleMapping.Role = role;
                    }
                }

                //await _userService.UpdateAsync(invitedUser.Id, invitedUser);
                await _invitationService.ExecuteRoleSwapAsync(invitedUser, null, invitation);

                _logger.LogInformation("Existing invited user updated: {UserId})", invitedUser.Id);

                await AuditLogs(invitation, invitedUser.Id!);

                return Ok(invitedUser.Id);
            }
            if (invitedUser != null && invitation.InvitedOrgId != null)
            {
                //Prepare Invited User (Gains Roles)
                invitedUser.Roles = MapAndFilterRoles(invitation.InvitedRoles);

                var rpReplaceRole = MapAndFilterRoles(invitation.RolesToReplace);

                //who is rp get rp user
                var rpuserId = invitation.ReplacedUserId;
                var rpUser = await _userService.GetByIdAsync(rpuserId);

                rpUser.Roles = MapAndFilterRoles(invitation.RolesToReplace);

                await _invitationService.ExecuteRoleSwapAsync(invitedUser, rpUser, invitation);

                //replace organisation RpUserId
                var organisation = await _organisationService.GetByOrgIdAsync(invitation.InvitedOrgId);
                organisation.RpUserId = invitedUser.Id;

                await _organisationService.UpdateAsync(organisation.Id, organisation);
                await AuditLogs(invitation, invitedUser.Id!);
                return StatusCode(StatusCodes.Status201Created, invitedUser.Id);
            }
            else
            {
                // Create new user from invitation
                var newUser = await BuildUserFromInvitation(request, invitation);
                await _invitationService.ExecuteRoleSwapAsync(newUser, null, invitation);
                _logger.LogInformation("New invited user registered: {UserId} (DB Id: {Id})", newUser.OneLoginId, newUser.Id);

                await AuditLogs(invitation, newUser.Id!);
                return StatusCode(StatusCodes.Status201Created, newUser.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during invited user registration. OneLoginId: {UserId}, Email: {Email}", request.OneLoginId, request.InvitedEmail);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while registering the invited user."
            });
        }
    }


    /// <summary>
    /// Gets list Contributor Roles from Enum class
    /// </summary>
    /// <returns>list Contributor Roles</returns>
    [HttpGet("contributor-roles")]
    [ProducesResponseType(typeof(List<EnumItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetContributorRoles()
    {
        var roles = EnumHelper.GetEnumItems<ContributorRole>();
        return Ok(roles);
    }


    /// <summary>
    /// Gets list Contributor Roles from Enum class
    /// </summary>
    /// <returns>list Contributor Roles</returns>
    [HttpGet("user-roles")]
    [ProducesResponseType(typeof(List<EnumItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetUserRoles()
    {
        var roles = EnumHelper.GetEnumItems<UserRole>();
        return Ok(roles);
    }




    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteUser(string id)
    {
        _logger.LogInformation("Attempting to delete user with ID: {UserId}", id);
        var existingUser = await _userService.GetByIdAsync(id);
        if (existingUser == null)
        {
            _logger.LogWarning("Delete request for user ID: {UserId} failed. User not found.", id);
            return NotFound();
        }

        try
        {
            await _userService.RemoveAsync(id);
            _logger.LogInformation("User with ID: {UserId} successfully removed.", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting user with ID: {UserId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while deleting the user.");
        }
    }


    [HttpGet("organisation/exists")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(bool))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<bool>> CheckOrganisationExistence(
        [FromQuery] string? companiesHouseNumber)
    {
        if (string.IsNullOrWhiteSpace(companiesHouseNumber))
        {
            _logger.LogWarning("Invalid request: 'companiesHouseNumber' query parameter is required.");
            return BadRequest("'companiesHouseNumber' must be provided.");
        }

        _logger.LogInformation("Checking if organisation with Companies House Number '{CompaniesHouseNumber}' has registered users.", companiesHouseNumber);

        try
        {
            bool exists = await _organisationService.IsOrganizationExists(companiesHouseNumber);
            _logger.LogInformation("Organisation exists: {Exists}", exists);
            return Ok(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while checking organisation existence.");
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
        }
    }

    [HttpGet("managed-users")]
    [ProducesResponseType(typeof(List<ManagedUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<ActionResult<List<ManagedUserResponse>>> GetManagedUsersAsync(string userId)
    {
        var user = await _userService.GetUserWithDetailsAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User with ID {UserId} not found.", userId);
            return NotFound();
        }

        // Map responsible user
        var managedUsers = new List<ManagedUserResponse>();
        var responsibleUser = _mapper.Map<ManagedUserResponse>(user);
        responsibleUser.HeatNetworks = MapHeatNetworks(user);
        managedUsers.Add(responsibleUser);

        // Get invitations and registered users
        var invitations = await _invitationService.GetInvitedUsersAsRegisteredAsync(user.Id);
        var invitedEmails = invitations.Select(i => i.EmailId).Distinct().ToList();
        var invitedUsersDetail = await _userService.GetUsersByInvitedEmailsWithDetailsAsync(invitedEmails);

        var registeredUsers = _mapper.Map<List<ManagedUserResponse>>(invitedUsersDetail);
        foreach (var ruser in registeredUsers)
        {
            var sourceUser = invitedUsersDetail.FirstOrDefault(x => x.Id == ruser.Id);
            ruser.HeatNetworks = MapHeatNetworks(sourceUser);
        }

        if (registeredUsers != null && registeredUsers.Any())
        {
            // Exclude the responsible user
            registeredUsers = registeredUsers.Where(ru => ru.EmailId != user.EmailId).ToList();
            managedUsers.AddRange(registeredUsers);
        }

        // Process invited users (latest per email/HN combination)
        var invitedUsers = invitations
            .GroupBy(i => new { i.EmailId, i.HeatNetworks?.FirstOrDefault()?.HnId })
            .Select(g => g.OrderByDescending(i => i.InvitedAt).First())
            .Where(i =>
                !registeredUsers.Any(u =>
                    u.EmailId == i.EmailId ||
                    (u.HeatNetworks?.Any(x => x.HnId == i.HeatNetworks?.FirstOrDefault()?.HnId) ?? false)
                )
                || i.Status == InvitationStatus.Invited.ToString()
            )
            .ToList();

        if (invitedUsers.Any())
        {
            managedUsers.AddRange(invitedUsers);
        }

        _logger.LogInformation("Managed users retrieved successfully for user ID: {UserId}", userId);
        return Ok(managedUsers);
    }

    [HttpGet("registered-users")]
    [ProducesResponseType(typeof(List<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<ActionResult<List<UserResponse>>> GetRegisteredUsersAsync(string userId)
    {
        var user = await _userService.GetByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User with ID {UserId} not found.", userId);
            return NotFound(); // Return 404 Not Found
        }

        _logger.LogInformation("Attempting to retrieve managed contributors for user ID: {UserId}", userId);

        var invitations = await _invitationService.GetByInvitedUserIdAsync(user.Id);
        var invitedEmails = invitations.Select(i => i.InvitedEmail).Distinct().ToList();

        var registeredUsers = await _userService.GetRegisteredUsers(invitedEmails);

        // Always check for null before calling .Any()
        if (registeredUsers == null || !registeredUsers.Any())
        {
            _logger.LogInformation("No contributors found for user ID: {UserId}", userId);
            return Ok(new List<UserResponse>()); // Return an empty list to avoid null reference issues
        }

        // Exclude the responsible user from the contributors list
        var filteredUsers = registeredUsers.Where(ru => ru.EmailId != user.EmailId).ToList();

        _logger.LogInformation("Successfully retrieved {Count} managed contributors for user ID: {UserId}", filteredUsers.Count, userId);

        return Ok(_mapper.Map<List<UserResponse>>(filteredUsers));
    }


    [HttpGet("heat-network/{hnId}/roles")]
    [ProducesResponseType(typeof(List<UserRoleDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<ActionResult<List<UserRoleDetailResponse>>> GetHeatNetworkUsersWithRoles(string hnId)
    {
        if (string.IsNullOrWhiteSpace(hnId))
        {
            return BadRequest("Heat Network ID must be provided.");
        }

        // Get Responsible Person
        var rpUser = await _userService.GetResponsiblePersonByHnIdAsync(hnId);
        if (rpUser == null)
        {
            return NotFound($"No Responsible Person found for Heat Network ID: {hnId}");
        }

        // Get other users with roles
        var result = await _userService.GetHeatNetworkUsersWithRolesAsync(hnId)
                     ?? new List<UserRoleDetailResponse>();

        // Insert RP user at the top
        result.Insert(0, _mapper.Map<UserRoleDetailResponse>(rpUser));

        return Ok(result);
    }


    /// <summary>
    /// Updates the OrgId associated with a specific user.
    /// </summary>
    /// <param name="request">The UserId and the NewOrgId.</param>
    [HttpPatch("update-orgid")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOrgId([FromBody] UpdateUserOrgIdRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _userService.UpdateOrgIdAsync(request.UserId, request.OrgId);

        if (result.IsAcknowledged == false)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Database update operation was not acknowledged.");
        }

        if (result.MatchedCount == 0)
        {
            return NotFound($"User with ID '{request.UserId}' not found.");
        }

        // 204 No Content is standard for a successful update where no data is returned
        return NoContent();
    }

    /// <summary>
    /// Gets all users belonging to a specific organisation ID.
    /// </summary>
    /// <param name="organisationId">The unique identifier of the organisation.</param>
    /// <returns>A list of User objects.</returns>
    [HttpGet("organisation/{organisationId}")] // Defines the HTTP GET route and parameter
    [ProducesResponseType(typeof(List<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<User>>> GetUsersByOrganisation(string organisationId)
    {
        if (string.IsNullOrEmpty(organisationId))
        {
            return BadRequest("Organisation ID cannot be empty.");
        }

        // Call the service method to fetch data from MongoDB
        var users = await _userService.GetUsersByOrgIdAsync(organisationId);

        if (users == null || !users.Any())
        {
            // Optional: return 404 if no users are found for that ID
            return NotFound($"No users found for organisation ID: {organisationId}");
        }

        var usersResponse = _mapper.Map<List<UserResponse>>(users);

        return Ok(usersResponse);
    }


    private static readonly Dictionary<ContributorRole, UserRole> RoleMapping =
        new Dictionary<ContributorRole, UserRole>
    {
            { ContributorRole.DesignatedDesigner, UserRole.DesignatedDutyHolder },
            { ContributorRole.DesignatedContractor, UserRole.DesignatedDutyHolder },
            { ContributorRole.DesignatedOperator, UserRole.DesignatedDutyHolder },
            { ContributorRole.ContributingDesigner, UserRole.Contributor },
            { ContributorRole.ContributingContractor, UserRole.Contributor },
            { ContributorRole.ContributingOperator, UserRole.Contributor },
            { ContributorRole.Assessor, UserRole.Assessor },
            { ContributorRole.Certifier, UserRole.Certifier },
            { ContributorRole.Coordinator, UserRole.Coordinator },
            { ContributorRole.ResponsiblePerson, UserRole.ResponsiblePerson }
    };


    private List<UserRole> MapAndFilterRoles(List<ContributorRole>? rolesToMap)
    {
        return rolesToMap?
            .Select(role =>
                RoleMapping.TryGetValue(role, out var mappedRole)
                ? (UserRole?)mappedRole
                : null
            )
            .Where(mappedRole => mappedRole.HasValue)
            .Select(mappedRole => mappedRole!.Value)
            .ToList()
            ?? new List<UserRole>();
    }

    private static List<HeatNetworkInfo> MapHeatNetworks(UserDetailsResult user)
    {
        var heatNetworks = GetHeatNetworksForUser(user);
        return heatNetworks?.Select(x => new HeatNetworkInfo
        {
            HnId = x.HnId,
            Name = x.Name
        }).ToList() ?? new List<HeatNetworkInfo>();
    }

    private static List<HeatNetworkUserResponse>? GetHeatNetworksForUser(UserDetailsResult src)
    {
        // Define the roles that grant access to the Organisation's full HeatNetwork list
        var rolesGrantingFullAccess = new List<UserRole> {
           UserRole.ResponsiblePerson,
           UserRole.Coordinator
        };

        // 1. Check for specific Heat Network role mappings (Highest Priority).
        if (src.HnRoleMappings != null && src.HnRoleMappings.Count > 0)
        {
            return src.HnRoleMappings.Select(m => m.HeatNetwork).ToList();
        }

        bool hasFullAccessRole = src.Roles != null &&
                                 src.Roles.Any(r => rolesGrantingFullAccess.Contains(r));

        if (hasFullAccessRole)
        {
            // If the user is RP or Coordinator, assign ALL heat networks from the organization.
            if (src.Organisation != null && src.Organisation.HeatNetworks != null)
            {
                return src.Organisation.HeatNetworks.ToList();
            }
        }

        return null;
    }

    private async Task<User> BuildUserFromInvitation(InvitedUserRequest request, Invitation invitation)
    {
        var user = new User
        {
            OneLoginId = request.OneLoginId,
            EmailId = request.InvitedEmail,
            FirstName = invitation.FirstName,
            LastName = invitation.LastName,
            JobTitle = null,
            Status = UserStatus.Active,
            OrgId = invitation.InvitedOrgId
        };

        if (invitation.InvitedHnId != null)
        {
            user.HnRoleMappings = new List<HnRoleMapping>
            {
                new HnRoleMapping
                {
                    HnId = invitation.InvitedHnId,
                    Role = invitation.InvitedRoles.FirstOrDefault()
                }
            };
        }

        user.Roles = MapAndFilterRoles(invitation.InvitedRoles);

        return user;
    }

    private async Task AuditLogs(Invitation invitation, string userId)
    {
        // Log for Audit history
        var isRegistrationEnabledString = Environment.GetEnvironmentVariable("IS_REGISTRATION_ENABLED");
        if (!string.IsNullOrEmpty(isRegistrationEnabledString) &&
                isRegistrationEnabledString.ToLower() == "true")

        {
            var existingHeatNetwork = await _heatNetworkService.GetByHnIdAsync(invitation.InvitedHnId!);
            if (existingHeatNetwork != null)
            {
                var phase = existingHeatNetwork.Phase;
                var stage = HeatNetworkHelper.GetStageFromPhase(phase);
                var invitedRole = invitation.InvitedRoles.FirstOrDefault();
                var entryType = "";
                switch (invitedRole)
                {
                    case ContributorRole.DesignatedDesigner:
                        entryType = "Designated designer assigned";
                        break;
                    case ContributorRole.DesignatedContractor:
                        entryType = "Designated contractor assigned";
                        break;
                    case ContributorRole.DesignatedOperator:
                        entryType = "Designated operator assigned";
                        break;
                    case ContributorRole.ContributingContractor:
                        entryType = "Contributor contractor assigned";
                        break;
                    case ContributorRole.ContributingDesigner:
                        entryType = "Contributor designer assigned";
                        break;
                    case ContributorRole.ContributingOperator:
                        entryType = "Contributor operator assigned";
                        break;
                    default:
                        break;
                }
                await _auditService.SaveAuditAsync<HeatNetwork>(
                    entryType: entryType,
                    actorId: userId,
                    entityId: existingHeatNetwork.HnId!,
                    oldState: null,
                    newState: existingHeatNetwork,
                    elementName: "NA",
                    phase: phase,
                    stage: stage
                );
            }
        }        
    }

}