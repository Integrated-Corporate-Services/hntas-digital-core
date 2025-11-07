using AutoMapper;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Helpers;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models;
using HNTAS.Core.Api.Models.Users;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace HNTAS.Core.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IOrganisationService _organizationService;
    private readonly IInvitationService _invitationService;
    private readonly ILogger<UsersController> _logger;
    private readonly ICounterService _orgCounterService;
    private readonly IMapper _mapper;
    private readonly IEmailService _emailService;


    public UsersController(IUserService userService,
                           IOrganisationService organizationService,
                           IInvitationService invitationService,
                           ILogger<UsersController> logger,
                           ICounterService orgCounterService,
                           IMapper mapper,
                           IEmailService emailService)
    {
        _userService = userService;
        _organizationService = organizationService;
        _invitationService = invitationService;
        _logger = logger;
        _emailService = emailService;
        _orgCounterService = orgCounterService;
        _mapper = mapper;
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
            var user = await _userService.GetUserWithDetailsAsync(id);
            _logger.LogInformation("Successfully retrieved {UserCount} users.", user.Id);
            return Ok(user);
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
    /// Check if a user is a Regulatory Contact by their email ID
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

        _logger.LogInformation("Checking if user with email ID {EmailId} is a Regulatory Contact.", sanitizedEmailId);
        try
        {
            var user = await _userService.GetByEmailAsync(emailId);

            if (user == null)
            {
                _logger.LogWarning("User with email ID {EmailId} not found.", sanitizedEmailId);
                return NotFound();
            }

            bool isRegulatoryContact = user.Roles.Contains(UserRole.RegulatoryContact);
            _logger.LogInformation("User with email ID {EmailId} is Regulatory Contact: {IsRp}", sanitizedEmailId, isRegulatoryContact);
            return Ok(isRegulatoryContact);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while checking Regulatory Contact role for email ID: {EmailId}", sanitizedEmailId);
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
                Status = registrationData.Status
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
    public async Task<ActionResult<User>> UpdateOrgDetails(string id, [FromBody] UpdateUserOrganisationRequest request)
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
                RegisteredAddress = _mapper.Map<RegisteredAddress>(request.Organisation.RegisteredAddress)
            };

            await _organizationService.CreateAsync(newOrg); // Save the new organization to its collection

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

            await _emailService.TrySendOrgCreatedEmailAsync(existingUser, newOrg); // Pass the new Org document

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
            await _invitationService.UpdateAsync(invitation.Id, invitation);

            // Check for existing user
            var existingUser = await _userService.GetByUserOneLoginIdAsync(request.OneLoginId);
            if (existingUser != null)
            {
                existingUser.HnIds ??= new List<string>();
                if (!existingUser.HnIds.Contains(invitation.InvitedHnId))
                {
                    existingUser.HnIds.Add(invitation.InvitedHnId);
                }
                // Add any new roles from the invitation
                existingUser.HnRoleMappings = existingUser.HnRoleMappings ?? new List<HnRoleMapping>();
                foreach (var role in invitation.InvitedRoles)
                {
                    // Update HnRoleMappings
                    var hnRoleMapping = existingUser.HnRoleMappings.FirstOrDefault(m => m.HnId == invitation.InvitedHnId);
                    if (hnRoleMapping == null)
                    {
                        hnRoleMapping = new HnRoleMapping
                        {
                            HnId = invitation.InvitedHnId,
                            Role = role,
                        };
                        existingUser.HnRoleMappings.Add(hnRoleMapping);
                    }
                    else if (!(hnRoleMapping.Role == role))
                    {
                        hnRoleMapping.Role = role;
                    }
                }
                await _userService.UpdateAsync(existingUser.Id, existingUser);

                _logger.LogInformation("Existing invited user updated: {UserId})", existingUser.Id);
                return Ok(existingUser.Id);
            }
            else
            {
                // Create new user from invitation
                var newUser = BuildUserFromInvitation(request, invitation);
                await _userService.CreateAsync(newUser);
                _logger.LogInformation("New invited user registered: {UserId} (DB Id: {Id})", newUser.OneLoginId, newUser.Id);
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

    // Remaining methods (UpdateHeatNetworkId, DeleteUser, CheckOrganisationExistence) are unchanged as they don't involve the nested documents that were moved.

    [HttpPatch("{id:length(24)}/heatnetwork/{heatNetworkId}")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> UpdateHeatNetworkId(string id, [FromRoute] string heatNetworkId)
    {
        try
        {
            var existingUser = await _userService.GetByIdAsync(id);
            if (existingUser == null)
            {
                _logger.LogWarning("User with ID {UserId} not found for heat network ID update.", id);
                return NotFound();
            }

            if (existingUser.HnIds == null)
            {
                existingUser.HnIds = new List<string>() { heatNetworkId };
            }
            else if (!existingUser.HnIds.Contains(heatNetworkId))
            {
                existingUser.HnIds.Add(heatNetworkId);
            }

            await _userService.UpdateAsync(id, existingUser);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating heat network ID for user with ID: {UserId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while updating the heat network ID.");
        }
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
            bool exists = await _organizationService.IsOrganizationExists(companiesHouseNumber);
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
            return null;
        }

        _logger.LogInformation("Successfully retrieved managed users for user ID: {UserId}", userId);



        var managedUsers = new List<ManagedUserResponse>
        {
            _mapper.Map<ManagedUserResponse>(user)
        };

        var invitations = await _invitationService.GetInvitedUsersAsRegisteredAsync(user.Id);
        var invitedEmails = invitations.Select(i => i.EmailId).Distinct().ToList();
        var registeredUsers = await _userService.GetRegisteredUsersDetailsAsync(invitedEmails);

        if (registeredUsers != null || registeredUsers.Any())
        {
            // Exclude the responsible user from the registered users list
            registeredUsers = registeredUsers.Where(ru => ru.EmailId != user.EmailId).ToList();
            managedUsers.AddRange(registeredUsers);
        }

        var invitedUsers = invitations.ToList()
        .Where(i =>
            !registeredUsers.Any(u =>
                u.EmailId == i.EmailId &&
                u.HeatNetworks.Any(x => x.HnId == i.HeatNetworks?.FirstOrDefault().HnId))).ToList();


        if (invitedUsers != null || invitedUsers.Count != 0)
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

    private User BuildUserFromInvitation(InvitedUserRequest request, Invitation invitation)
    {
        var roles = new List<UserRole> { };
        foreach (var role in invitation.InvitedRoles)
        {
            if (role == ContributorRole.DesignatedDesigner || role == ContributorRole.ContributingDesigner)
            {
                roles.Add(UserRole.Designer);
            }
            else if (role == ContributorRole.DesignatedContractor || role == ContributorRole.ContributingContractor)
            {
                roles.Add(UserRole.Contractor);
            }
            else if (role == ContributorRole.DesignatedOperator || role == ContributorRole.ContributingOperator)
            {
                roles.Add(UserRole.Operator);
            }
            else if (role == ContributorRole.Assessor)
            {
                roles.Add(UserRole.Assessor);
            }
            else if (role == ContributorRole.Certifier)
            {
                roles.Add(UserRole.Certifier);
            }
            else
            {
                roles.Add(UserRole.Contributor);
            }
        }
        var user = new User
        {
            OneLoginId = request.OneLoginId,
            EmailId = request.InvitedEmail,
            FirstName = invitation.FirstName,
            LastName = invitation.LastName,
            JobTitle = null,
            Status = UserStatus.Active,
            OrgId = request.InviterOrgId,
            Roles = roles,
            HnIds = [invitation.InvitedHnId],
            HnRoleMappings = [new HnRoleMapping
                                {
                                    HnId = invitation.InvitedHnId,
                                    Role = invitation.InvitedRoles.FirstOrDefault()
                                }
                            ]
        };

        return user;
    }
}