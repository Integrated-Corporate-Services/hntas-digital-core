using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HNTAS.Core.Api.Services
{
    public class InvitationService : IInvitationService
    {
        private readonly IMongoCollection<Invitation> _invitationsCollection;
        private readonly ILogger<InvitationService> _logger;
        private readonly IMongoClient _mongoClient;
        private readonly IUserService _userService;

        public InvitationService(
        IMongoDatabase mongoDatabase,
        IOptions<AWSDocDbSettings> dbSettings,
        ILogger<InvitationService> logger,
        IMongoClient mongoClient,
        IUserService userService)
        {
            _logger = logger;
            _invitationsCollection = mongoDatabase.GetCollection<Invitation>(dbSettings.Value.InvitationsCollectionName);
            _logger.LogInformation("UserService initialized via Dependency Injection.");
            _mongoClient = mongoClient;
            _userService = userService;
        }

        // Get all invitations
        public async Task<List<Invitation>> GetAsync() =>
            await _invitationsCollection.Find(_ => true).ToListAsync();

        // Get invitation by ID
        public async Task<Invitation> GetByIdAsync(string id) =>
            await _invitationsCollection.Find(invitation => invitation.Id == id).FirstOrDefaultAsync();

        // Get invitations by InviterUserId
        public async Task<List<Invitation>> GetByInvitedUserIdAsync(string inviterUserId) =>
            await _invitationsCollection.Find(invitation => invitation.InviterUserId == inviterUserId).ToListAsync();

        // Create a new invitation
        public async Task CreateAsync(Invitation newInvitation) =>
            await _invitationsCollection.InsertOneAsync(newInvitation);

        // Update an existing invitation
        public async Task UpdateAsync(string id, Invitation updatedInvitation) =>
            await _invitationsCollection.ReplaceOneAsync(invitation => invitation.Id == id, updatedInvitation);

        // Remove an invitation by ID
        public async Task RemoveAsync(string id) =>
            await _invitationsCollection.DeleteOneAsync(invitation => invitation.Id == id);

        public async Task<Invitation> GetByEmailAsync(string invitedEmail, string hnId) =>
          await _invitationsCollection
              .Find(invitation =>
                  invitation.InvitedEmail == invitedEmail &&
                  invitation.InvitedHnId == hnId &&
                  invitation.Status == Enums.InvitationStatus.Invited)
              .SortByDescending(invitation => invitation.InvitedAt)
              .FirstOrDefaultAsync();

        public async Task<List<Invitation>> GetByEmailsAndHnIdAsync(List<string> invitedEmails, string hnId) =>
             await _invitationsCollection
                .Find(invitation =>
                    invitedEmails.Contains(invitation.InvitedEmail) &&
                    invitation.InvitedHnId == hnId &&
                    invitation.Status == Enums.InvitationStatus.Accepted)                
                .ToListAsync();

        public async Task<List<ManagedUserResponse>> GetInvitedUsersAsRegisteredAsync(string inviterUserId)
        {
            var inviterObjectId = ObjectId.Parse(inviterUserId);

            var pipeline = new[]
            {
                // Match invitations sent by the specified user
                new BsonDocument("$match", new BsonDocument("inviterUserId", inviterObjectId)),

                // Lookup heat network name using invitedHnId
                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "HeatNetworks" },
                    { "localField", "invitedHnId" },
                    { "foreignField", "hnId" },
                    { "as", "heatNetworkDetails" }
                }),

                // Sort by invitedAt descending
                new BsonDocument("$sort", new BsonDocument("invitedAt", -1)),

                // Project into RegisteredUserResponse shape
                new BsonDocument("$project", new BsonDocument
                {
                    { "_id", new BsonDocument("$toString", "$_id") },
                    { "name", new BsonDocument("$concat", new BsonArray { "$firstName", " ", "$lastName" }) },
                    { "emailId", "$invitedEmail" },
                    { "invitedAt", "$invitedAt" },
                    { "status", new BsonDocument("$toString", "$status") },
                    { "roles", new BsonDocument("$map", new BsonDocument
                        {
                            { "input", "$invitedRoles" },
                            { "as", "role" },
                            { "in", new BsonDocument("$toString", "$$role") }
                        })
                    },
                    { "heatNetworks", new BsonDocument("$map", new BsonDocument
                        {
                            { "input", "$heatNetworkDetails" },
                            { "as", "hn" },
                            { "in", new BsonDocument
                                {
                                    { "hnId", "$$hn.hnId" },
                                    { "name", "$$hn.name" }
                                }
                            }
                        })
                    }
                })
            };

            return await _invitationsCollection
                .Aggregate<ManagedUserResponse>(pipeline)
                .ToListAsync();
        }

        public async Task ExecuteRoleSwapAsync(User invitedUser, User? replacedUser, Invitation invitation)
        {
            try
            {
                if (invitedUser?.Id == null)
                    await _userService.CreateAsync(invitedUser);
                else
                    await _userService.UpdateAsync(invitedUser?.Id, invitedUser);

                if (replacedUser != null)
                    await _userService.UpdateAsync(replacedUser?.Id, replacedUser);

                if (invitation != null)
                    await UpdateAsync(invitation.Id, invitation);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to complete role swap transaction.");
                throw;
            }

        }

        // Get invitation by invitedEmailId, invitedHnId, invitedRole
        public async Task<Invitation> GetByInvitedDetailsAsync(string invitedEmailId, string invitedHnId, ContributorRole invitedRole) =>
            await _invitationsCollection.Find(invitation => invitation.InvitedEmail == invitedEmailId && invitation.InvitedHnId == invitedHnId && invitation.InvitedRoles.Contains(invitedRole) && invitation.Status == Enums.InvitationStatus.Accepted).FirstOrDefaultAsync();

        public async Task<Invitation> GetByInvitedEmailAsync(string invitedEmailId) =>
            await _invitationsCollection.Find(invitation => invitation.InvitedEmail == invitedEmailId).FirstOrDefaultAsync();
    }
}
