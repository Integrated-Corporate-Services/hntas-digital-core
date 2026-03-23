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
        /// <param name="entryType">The narrative intent (e.g., "HeatNetworkCharacteristicsUpdated")</param>
        /// <param name="actorId">The ID or Email of the user performing the action</param>
        /// <param name="entityId">The unique identifier of the entity being audited</param>
        /// <param name="oldState">The object state before the change</param>
        /// <param name="newState">The object state after the change</param>
        /// <param name="changeNote">Optional human-readable reason for the change</param>
        /// <param name="elementName">The specific element being changed</param>
        /// <param name="phase">The phase of the heat network</param>
        /// <param name="stage">The stage of the heat network</param>
        Task SaveAuditAsync<T>(
            string entryType,
            string actorId,
            string entityId,
            T? oldState,
            T? newState,            
            string elementName,
            string phase,
            string stage,
            string? changeNote = null);

        /// <summary>
        /// Retrieves the history for a specific entity from its dedicated audit collection.
        /// </summary>
        Task<List<AuditEntry<T>>> GetHistoryAsync<T>(string entityId);


        Task<List<AuditLogResponse>> GetAuditHistoryAsync<T>(string entityId);
    }
}
