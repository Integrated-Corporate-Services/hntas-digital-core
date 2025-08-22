using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Interfaces;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace HNTAS.Core.Api.Services
{
    public class InvitationService : IInvitationService
    {
        private readonly IMongoCollection<Invitation> _invitationsCollection;
        private readonly ILogger<InvitationService> _logger;


        public InvitationService(IOptions<AWSDocDbSettings> dbSettings, ILogger<InvitationService> logger)
        {
            _logger = logger;
            string? connectionString = Environment.GetEnvironmentVariable("DOCUMENT_DB_CONNECTION_STRING");
            _logger.LogInformation("Initializing InvitationService with connection string: {connectionString}", connectionString);

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("MongoDB connection string is not configured. Set 'DOCUMENT_DB_CONNECTION_STRING' environment variable");
            }

            var mongoClient = new MongoClient(connectionString);
            var mongoDatabase = mongoClient.GetDatabase(dbSettings.Value.DatabaseName);

            _invitationsCollection = mongoDatabase.GetCollection<Invitation>(dbSettings.Value.InvitationsCollectionName);
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
    }
}
