using HNTAS.Core.Api.Constants;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Helpers;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models;
using HNTAS.Core.Api.Models.Soa;
using HNTAS.Core.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;

namespace HNTAS.Core.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SOAController : ControllerBase
    {
        private readonly ISoaService _soaService;
        private readonly ILogger<SOAController> _logger;
        private readonly IEmailService _emailService;
        private readonly IHeatNetworkService _heatNetworkService;
        private readonly IUserService _userService;
        private readonly IAuditService _auditService;
        private readonly INotificationHistoryService _notificationHistoryService;
        private readonly IInvitationService _invitationService;
        public SOAController(ISoaService soaProjectService, ILogger<SOAController> logger, IEmailService emailService, IHeatNetworkService heatNetworkService, IUserService userService, IAuditService auditService, INotificationHistoryService notificationHistoryService, IInvitationService invitationService)
        {
            _soaService = soaProjectService;
            _logger = logger;
            _emailService = emailService;
            _heatNetworkService = heatNetworkService;
            _userService = userService;
            _auditService = auditService;
            _notificationHistoryService = notificationHistoryService;
            _invitationService = invitationService;
        }


        [HttpGet("heat-network/{hnId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Soa))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Soa>> GetByHeatNetworkIdAsync(string hnId)
        {
            _logger.LogInformation("Retrieving SOA project for Heat Network ID: {HnId}", hnId);

            var project = await _soaService.GetByHeatNetworkIdAsync(hnId);

            if (project is null)
            {
                _logger.LogWarning("SOA project not found for Heat Network ID: {HnId}", hnId);
                return NotFound();
            }

            _logger.LogInformation("SOA project retrieved successfully for Heat Network ID: {HnId}", hnId);
            return Ok(project);
        }


        [HttpPost("create")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Soa))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateProject([FromQuery] string hnId, [FromQuery] string createdBy)
        {
            if (string.IsNullOrWhiteSpace(hnId))
            {
                _logger.LogWarning("Create request rejected: missing Heat Network ID.");
                return BadRequest("Heat Network ID is required.");
            }

            if (string.IsNullOrWhiteSpace(createdBy))
            {
                _logger.LogWarning("Create request rejected: missing CreatedBy.");
                return BadRequest("CreatedBy is required.");
            }

            _logger.LogInformation("Initiating SOA creation for Heat Network ID: {HnId} by user: {CreatedBy}", hnId, createdBy);

            var soa = await _soaService.CreateAsync(hnId, createdBy);

            if (soa is null)
            {
                _logger.LogWarning("SOA creation failed for Heat Network ID: {HnId}", hnId);
                return BadRequest("Unable to create SOA data.");
            }

            _logger.LogInformation("SOA data successfully created for Heat Network ID: {HnId}", hnId);
            return Ok(soa);
        }




        [HttpPatch("connections")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateConnections([FromBody] UpdateConnectionsRequest request)
        {
            _logger.LogInformation("Updating connection types for Heat Network ID: {HnId} by {UpdatedBy}", request.HnId, request.UpdatedBy);

            if (string.IsNullOrEmpty(request.HnId))
            {
                _logger.LogWarning("Heat Network ID is missing in connection update.");
                return BadRequest("Heat Network ID is required.");
            }

            if (string.IsNullOrEmpty(request.UpdatedBy))
            {
                _logger.LogWarning("UpdatedBy is missing in connection update.");
                return BadRequest("UpdatedBy is required.");
            }

            var project = await _soaService.GetByHeatNetworkIdAsync(request.HnId);
            if (project == null)
            {
                _logger.LogWarning("SOA project not found for connection update: {HnId}", request.HnId);
                return NotFound();
            }

            await _soaService.UpdateConnectionTypesAsync(request.HnId, request.UpdatedBy, request.ConnectionTypes);
            _logger.LogInformation("Connection types updated for Heat Network ID: {HnId}", request.HnId);

            return Ok();
        }


        [HttpPatch("network-type")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateNetworkType([FromQuery] string hnId, [FromQuery] string updatedBy, [FromBody] NetworkTypeSelection networkTypeSelection)
        {
            _logger.LogInformation("Updating network type for Heat Network ID: {hnId} by {UpdatedBy}", hnId, updatedBy);

            if (string.IsNullOrEmpty(hnId))
            {
                _logger.LogWarning("Heat Network ID is missing in network type update.");
                return BadRequest("Heat Network ID is required.");
            }

            if (string.IsNullOrEmpty(updatedBy))
            {
                _logger.LogWarning("UpdatedBy is missing in network type update.");
                return BadRequest("UpdatedBy is required.");
            }

            var project = await _soaService.GetByHeatNetworkIdAsync(hnId);
            if (project == null)
            {
                _logger.LogWarning("SOA project not found for network type update: {hnId}", hnId);
                return NotFound();
            }

            await _soaService.UpdateNetworkTypeAsync(hnId, updatedBy, networkTypeSelection);
            _logger.LogInformation("Network type updated for Heat Network ID: {hnId}", hnId);

            return Ok();
        }

        [HttpPatch("network-elements")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateNetworkElements([FromQuery] string hnId, [FromQuery] string updatedBy, [FromBody] List<HeatNetworkElement> networkElements)
        {
            _logger.LogInformation("Updating network type for Heat Network ID: {hnId} by {UpdatedBy}", hnId, updatedBy);

            if (string.IsNullOrEmpty(hnId))
            {
                _logger.LogWarning("Heat Network ID is missing in network type update.");
                return BadRequest("Heat Network ID is required.");
            }

            if (string.IsNullOrEmpty(updatedBy))
            {
                _logger.LogWarning("UpdatedBy is missing in network type update.");
                return BadRequest("UpdatedBy is required.");
            }

            var project = await _soaService.GetByHeatNetworkIdAsync(hnId);
            if (project == null)
            {
                _logger.LogWarning("SOA project not found for network type update: {hnId}", hnId);
                return NotFound();
            }

            await _soaService.UpdateHeatNetworkElementsAsync(hnId, networkElements, updatedBy);
            _logger.LogInformation("Network type updated for Heat Network ID: {hnId}", hnId);

            return Ok();
        }

        [HttpPatch("element-locations")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SaveElementLocations([FromBody] UpdateElementLocationsRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid SaveLocations request: {@Errors}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Saving locations for element type: {ElementType} in HN ID: {HnId} by {UpdatedBy}. Location count: {LocationCount}",
                request.ElementType, request.HnId, request.UpdatedBy, request.Locations?.Count ?? 0);

            var project = await _soaService.GetByHeatNetworkIdAsync(request.HnId);
            if (project == null)
            {
                _logger.LogWarning("SOA project not found for location save: {HnId}", request.HnId);
                return NotFound();
            }

            try
            {
                await _soaService.UpdateElementLocationsAsync(request.HnId, request.ElementType, request.Locations, request.UpdatedBy);
                _logger.LogInformation("Locations updated successfully for element type: {ElementType} in HN ID: {HnId} by {UpdatedBy}",
                    request.ElementType, request.HnId, request.UpdatedBy);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update locations for element type: {ElementType} in HN ID: {HnId} by {UpdatedBy}",
                    request.ElementType, request.HnId, request.UpdatedBy);
                throw;
            }
        }

        [HttpPatch("element-documents")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SaveElementDocuments([FromBody] UpdateElementDocumentsRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid SaveDocuments request: {@Errors}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Saving documents for element type: {ElementType} in HN ID: {HnId} by {UpdatedBy}. Document count: {DocumentCount}",
                request.ElementType, request.HnId, request.UpdatedBy, request.Documents?.Count ?? 0);

            var project = await _soaService.GetByHeatNetworkIdAsync(request.HnId);
            if (project == null)
            {
                _logger.LogWarning("SOA project not found for document save: {HnId}", request.HnId);
                return NotFound();
            }

            try
            {
                await _soaService.UpdateElementDocumentsAsync(request.HnId, request.ElementType, request.Documents, request.UpdatedBy);
                _logger.LogInformation("Documents updated successfully for element type: {ElementType} in HN ID: {HnId} by {UpdatedBy}",
                    request.ElementType, request.HnId, request.UpdatedBy);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update documents for element type: {ElementType} in HN ID: {HnId} by {UpdatedBy}",
                    request.ElementType, request.HnId, request.UpdatedBy);
                throw;
            }
        }


        [HttpPatch("document-update")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SaveDocument([FromBody] UpdateDocumentRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid SaveDocument request: {@Errors}",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Saving {DocumentType} document for HN ID: {HnId}, Phase: {Phase}, Stage: {Stage}, UploadedBy: {UploadedBy}",
                request.DocumentType, request.HnId, request.Phase, request.Stage, request.UploadedBy);

            var project = await _soaService.GetByHeatNetworkIdAsync(request.HnId);
            if (project == null)
            {
                _logger.LogWarning("SOA not found for {DocumentType} document save: {HnId}", request.DocumentType, request.HnId);
                return NotFound();
            }

            var document = new Document
            {
                FileName = request.FileName,
                S3Key = request.S3Key,
                Phase = request.Phase,
                Stage = request.Stage,
                UploadedAt = DateTime.UtcNow,
                UploadedBy = request.UploadedBy
            };

            try
            {
                switch (request.DocumentType)
                {
                    case DocumentType.Assessment:
                        await _soaService.UpdateAssessmentDocumentAsync(request.HnId, document);
                        break;
                    case DocumentType.Assessor:
                        await _soaService.UpdateAssessorDocumentAsync(request.HnId, document);
                        break;
                    case DocumentType.Certifier:
                        await _soaService.UpdateCertifierDocumentAsync(request.HnId, document);
                        break;                     
                    default:
                        _logger.LogWarning("Unsupported document type: {DocumentType}", request.DocumentType);
                        return BadRequest($"Unsupported document type: {request.DocumentType}");
                }

                _logger.LogInformation("{DocumentType} document saved successfully for HN ID: {HnId}, Phase: {Phase}, Stage: {Stage}, UploadedBy: {UploadedBy}",
                    request.DocumentType, request.HnId, request.Phase, request.Stage, request.UploadedBy);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save {DocumentType} document for HN ID: {HnId}, Phase: {Phase}, Stage: {Stage}, UploadedBy: {UploadedBy}",
                    request.DocumentType, request.HnId, request.Phase, request.Stage, request.UploadedBy);
                throw;
            }
        }        

        [HttpPatch("update-soa-status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateSoaStatus([FromBody] ElementSoaStatusUpdateRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid SaveDocument request: {@Errors}",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Saving statuses for HN ID: {HnId}, Element:{ElementId}, Stage: {Stage}, UpdatedBy: {UpdatedBy}",
                 StringFormatter.Sanitize(request.HnId), StringFormatter.Sanitize(request.ElementId!), request.Stage, StringFormatter.Sanitize(request.SoaStatusUpdatedBy!));

            try
            {
                var existingHeatNetwork = await _heatNetworkService.GetByHnIdAsync(request.HnId);
                if (existingHeatNetwork == null)
                {
                    _logger.LogInformation("No heat network found for HnId: {HnId}", StringFormatter.Sanitize(request.HnId));
                    return NotFound($"No heat network found for HnId '{request.HnId}'.");
                }

                await _soaService.UpdateSoaStatus(request.HnId, request.ElementType, request.Stage, request.SoaStatuses!, request.SoaStatusUpdatedBy!, request.ElementSoaStatus);

                _logger.LogInformation("Updated statuses successfully for HN ID: {HnId}, Element:{ElementId}, Stage: {Stage}, UpdatedBy: {UpdatedBy}",
                 StringFormatter.Sanitize(request.HnId), StringFormatter.Sanitize(request.ElementId!), request.Stage, StringFormatter.Sanitize(request.SoaStatusUpdatedBy!));

                var isRegistrationEnabledString = Environment.GetEnvironmentVariable("IS_REGISTRATION_ENABLED");
                if (!string.IsNullOrEmpty(isRegistrationEnabledString) &&
                    isRegistrationEnabledString.ToLower() == "true")
                {
                    var updatedHeatNetwork = await _heatNetworkService.GetByHnIdAsync(request.HnId);

                    await _auditService.SaveAuditAsync<HeatNetwork>(
                        entryType: "SOA - " + request.SoaStatuses,
                        actorId: request.SoaStatusUpdatedBy!,
                        entityId: existingHeatNetwork.HnId!,
                        oldState: existingHeatNetwork,
                        newState: updatedHeatNetwork,
                        elementName: request.ElementDisplayName!,
                        phase: request.SoaPhase!,
                        stage: request.Stage.ToString()
                    );
                }
                    

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update statuses for HN ID: {HnId}, Element:{ElementId}, Stage: {Stage}, UpdatedBy: {UpdatedBy}",
                 StringFormatter.Sanitize(request.HnId), StringFormatter.Sanitize(request.ElementId!), request.Stage, StringFormatter.Sanitize(request.SoaStatusUpdatedBy!));
                throw;
            }
        }

        [HttpPatch("soa-assign-assessor")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SoaAssignAssessor([FromBody] ElementSoaAssignAssessorRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid SaveDocument request: {@Errors}",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Saving Assessor Assigned for HN ID: {HnId}, UpdatedBy: {UpdatedBy}",
                StringFormatter.Sanitize(request.HnId), StringFormatter.Sanitize(request.UpdatedBy));

            try
            {
                var existingHeatNetwork = await _heatNetworkService.GetByHnIdAsync(request.HnId);
                if (existingHeatNetwork == null)
                {
                    _logger.LogInformation("No heat network found for HnId: {HnId}", StringFormatter.Sanitize(request.HnId));
                    return NotFound($"No heat network found for HnId '{request.HnId}'.");
                }

                var networkElements = existingHeatNetwork.NetworkElements;

                await _soaService.UpdateAssignAssessor(request, networkElements!, existingHeatNetwork.Phase, true);
                existingHeatNetwork = await _heatNetworkService.GetByHnIdAsync(request.HnId);
                networkElements = existingHeatNetwork.NetworkElements;
                var elementModelToUpdate = await _soaService.UpdateAssignAssessor(request, networkElements!, existingHeatNetwork.Phase, false);
                existingHeatNetwork.NetworkElements = elementModelToUpdate;
                await _heatNetworkService.UpdateAsync(request.HnId, existingHeatNetwork);
                await NotificationHistoryForAssigningAssessor(request, existingHeatNetwork);
                _logger.LogInformation("Saved Assessor Assigned for HN ID: {HnId}, UpdatedBy: {UpdatedBy}",
                StringFormatter.Sanitize(request.HnId), StringFormatter.Sanitize(request.UpdatedBy));

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save Assessor Assigned for HN ID: {HnId}, UpdatedBy: {UpdatedBy}",
                StringFormatter.Sanitize(request.HnId), StringFormatter.Sanitize(request.UpdatedBy));
                throw;
            }
        }

        [HttpDelete("{hnId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteSoaProject(string hnId)
        {
            if (string.IsNullOrWhiteSpace(hnId))
            {
                _logger.LogWarning("Delete request received with empty HN ID.");
                return BadRequest("Heat Network ID is required.");
            }

            _logger.LogInformation("Attempting to delete SOA project for HN ID: {HnId}", hnId);

            var project = await _soaService.GetByHeatNetworkIdAsync(hnId);
            if (project == null)
            {
                _logger.LogWarning("SOA project not found for deletion: {HnId}", hnId);
                return NotFound();
            }

            try
            {
                await _soaService.DeleteByHeatNetworkIdAsync(hnId);
                _logger.LogInformation("SOA project deleted successfully for HN ID: {HnId}", hnId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete SOA project for HN ID: {HnId}", hnId);
                throw;
            }
        }


        [HttpPut("update-soa-status")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateSoaStatus([FromBody] UpdateSoaStatusRequest request)
        {
            _logger.LogInformation("Updating SOA status to {Status} for HN ID: {HnId} by {UpdatedBy}", request.Status, request.HnId, request.UpdatedBy);

            if (string.IsNullOrWhiteSpace(request.HnId))
                return BadRequest("Heat Network ID is required.");

            if (string.IsNullOrWhiteSpace(request.HnName))
                return BadRequest("Heat Network Name is required.");

            if (string.IsNullOrWhiteSpace(request.UpdatedBy))
                return BadRequest("UpdatedBy is required.");

            if (!Enum.IsDefined(typeof(SoaStatus), request.Status))
                return BadRequest($"Invalid SOA status: {request.Status}");

            var soa = await _soaService.UpdateStatusAsync(request.HnId, request.Status, request.UpdatedBy);
            var user = await _userService.GetUserWithDetailsAsync(request.UpdatedBy);

            if (soa == null)
            {
                _logger.LogWarning("No SOA found to update for HN ID: {HnId}", request.HnId);
                return BadRequest("SOA not found.");
            }

            var users = await _userService.GetAssessorsByHnIdAsync(request.HnId);
            var assessor = users.FirstOrDefault();
            if (assessor == null)
            {
                _logger.LogWarning("No assessor found for HN ID: {HnId}", request.HnId);
                return NotFound();
            }

            //if (request.Status == SoaStatus.Submitted)
            //{
                //send email
                await _emailService.TrySendAssessorEmailAsync(
                         emailAddress: assessor.EmailId,
                         hnName: request.HnName,
                         hnId: request.HnId,
                         contributorName: user?.FullName
                     );
            //}

            return NoContent();
        }


        /// <summary>
        /// Sends an assessment result email to all assessors linked to the specified heat network.
        /// </summary>
        /// <param name="hnName">Heat network name.</param>
        /// <param name="hnId">Heat network ID.</param>
        /// <param name="assessmentResult">The result of the assessment (e.g., Pass, Fail).</param>
        /// <returns>204 No Content if successful; 400 for invalid input; 500 for unexpected errors.</returns>
        [HttpPost("send-assessor-assessment-email")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendAssessorAssessmentEmail(
            [FromQuery][Required] string hnName,
            [FromQuery][Required] string hnId,
            [FromQuery][Required] string assessmentResult)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid assessor assessment email request: hnName={HnName}, hnId={HnId}, result={Result}", hnName, hnId, assessmentResult);
                return BadRequest(ModelState);
            }

            try
            {
                var rpUser = await _userService.GetResponsiblePersonByHnIdAsync(hnId);
                var contributorUsers = await _userService.GetContributorsByHnIdAsync(hnId);

                var contributorEmails = contributorUsers
                .Where(c => c.HnRoleMappings.Any(m => m.HnId == hnId && (m.Role != ContributorRole.Assessor && m.Role != ContributorRole.Certifier))).Select(c => c.EmailId)
                .Distinct()
                .ToList();

                if (!contributorEmails.Any())
                {
                    _logger.LogWarning("No assessors found for HN ID: {HnId}", hnId);
                    return NotFound();
                }

                foreach (var email in contributorEmails)
                {
                    await _emailService.TrySendAssessorAssessmentEmailAsync(email, hnName, hnId, assessmentResult);
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending assessment result email for HN ID: {HnId}", hnId);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }



        /// <summary>
        /// Sends a certification complete email to the specified recipient.
        /// </summary>
        /// <param name="hnName">Heat network name.</param>
        /// <param name="hnId">Heat network ID.</param>
        /// <returns>204 No Content if successful; 400 for invalid input; 500 for unexpected errors.</returns>
        [HttpPost("send-certification-complete-email")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendCertificationCompleteEmail(
            [FromQuery][Required] string hnName,
            [FromQuery][Required] string hnId)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid certification complete email request: hnName={HnName}, hnId={HnId}", hnName, hnId);
                return BadRequest(ModelState);
            }

            try
            {
                List<string> emailRecipients = new List<string>();
                var rpUser = await _userService.GetResponsiblePersonByHnIdAsync(hnId);
                var contributorUsers = await _userService.GetContributorsByHnIdAsync(hnId);

                contributorUsers = contributorUsers
                    .Where(c => c.HnRoleMappings.Any(m => m.HnId == hnId && (m.Role != ContributorRole.Assessor && m.Role != ContributorRole.Certifier))).ToList();

                if (rpUser != null)
                {
                    emailRecipients.Add(rpUser.EmailId);
                }
                if (contributorUsers != null && contributorUsers.Count > 0)
                {
                    emailRecipients.AddRange(contributorUsers.Select(c => c.EmailId));
                }

                foreach (var email in emailRecipients.Distinct())
                {
                    await _emailService.TrySendCertificationCompleteEmailAsync(email, hnName, hnId);
                    _logger.LogInformation("Certification complete email sent to {EmailAddress} for HN ID: {HnId}", email, hnId);
                }


                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending certification complete email to {hnName}", hnName);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        private async Task NotificationHistoryForAssigningAssessor(ElementSoaAssignAssessorRequest request, HeatNetwork heatNetwork)
        {
            // Get user's email and role
            var currentUser = await _userService.GetUserWithDetailsAsync(request.UpdatedBy);
            var rpUserId = "";
            var nmUserId = "";
            if (currentUser.Roles!.Contains(UserRole.ResponsiblePerson))
            {
                rpUserId = currentUser.Id!;
            }
            else
            {
                var invitaions = await _invitationService.GetByInvitedEmailAsync(currentUser.EmailId!);
                nmUserId = currentUser.Id!;
                rpUserId = invitaions.InviterUserId;
            }

            var users = await _userService.GetUsersAssociatedByHnIdAsync(request.HnId);
            // get distinct user emailIds from the list of users
            var emailIds = users.Select(u => u.EmailId).Distinct().ToList();
            var userIds = users.Select(u => u.Id).Distinct().ToList();

            var acceptedInvitations = await _invitationService.GetByEmailsAndHnIdAsync(emailIds, request.HnId);
            var invitorUserIds = acceptedInvitations.Select(i => i.InviterUserId).Distinct().ToList();

            // merge userIds and invitorUserIds and get distinct list of userIds to be notified
            var actors = userIds.Union(invitorUserIds).Distinct().ToList();
            actors.Add(rpUserId);
            if (!string.IsNullOrEmpty(nmUserId))
            {
                actors.Add(nmUserId);
            }
            // check if request.UpdatedBy is in actors list, if not add to the list
            //actors = actors.Contains(request.UpdatedBy) ? actors : actors.Append(request.UpdatedBy).ToList();

            //var description = $"{request.AssessorFirstName} {request.AssessorLastName} Assigned to {heatNetwork.HnId}-{heatNetwork.Name}";
            // TODO: Update description to include assessors name;
            var description = "";
            var eligibleRoles = new List<string> { ContributorRole.ResponsiblePerson.ToString()
                , ContributorRole.NetworkManager.ToString(),
                ContributorRole.DesignatedDesigner.ToString(),
                ContributorRole.DesignatedContractor.ToString(),
                ContributorRole.DesignatedOperator.ToString(),
                ContributorRole.ContributingDesigner.ToString(),
                ContributorRole.ContributingContractor.ToString(),
                ContributorRole.ContributingOperator.ToString()};
            var notificationHistory = new NotificationHistory
            {
                NotificationType = NotificationHistoryType.AssessorAssignsToHeatNetwork,
                ActorsId = actors!,
                Subject = NotificationHistorySubjects.AssessorAssignedToHN,
                Description = description,
                Timestamp = DateTime.UtcNow,
                Action = NotificationHistoryActions.HeatNetworkDetails,
                HeatNetworkId = heatNetwork.HnId,
                CreatedBy = request.UpdatedBy,
                EligibleRoles = eligibleRoles
            };
            await _notificationHistoryService.CreateAsync(notificationHistory);
        }

    }
}