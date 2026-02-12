using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Models;

namespace HNTAS.Core.Api.Interfaces
{
    public interface IAuditService
    {
        /// <summary>
        /// Saves a point-in-time snapshot of an entity before and after a change.
        /// </summary>
        /// <typeparam name="T">The entity type (e.g., HeatNetwork)</typeparam>
        /// <param name="eventName">The narrative intent (e.g., "HeatNetworkCharacteristicsUpdated")</param>
        /// <param name="actorId">The ID or Email of the user performing the action</param>
        /// <param name="entityId">The unique identifier of the entity being audited</param>
        /// <param name="oldState">The object state before the change</param>
        /// <param name="newState">The object state after the change</param>
        /// <param name="changeNote">Optional human-readable reason for the change</param>
        Task SaveAuditAsync<T>(
            string eventName,
            string actorId,
            string entityId,
            T? oldState,
            T? newState,
            string? changeNote = null);

        /// <summary>
        /// Retrieves the history for a specific entity from its dedicated audit collection.
        /// </summary>
        Task<List<AuditEntry<T>>> GetHistoryAsync<T>(string entityId);


        Task<List<AuditLogResponse>> GetAuditHistoryAsync<T>(string entityId);
    }
}
