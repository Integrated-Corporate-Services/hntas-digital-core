namespace HNTAS.Core.Api.Interfaces
{
    /// <summary>
    /// Defines the contract for a service that manages sequential counters specifically for Organisation-related IDs.
    /// </summary>
    public interface ICounterService
    {
        /// <summary>
        /// Atomically increments and returns the next sequence value for a given Organisation-related counter.
        /// This is typically used to generate unique, sequential numeric IDs (e.g., for 'org_id').
        /// </summary>
        /// <param name="sequenceName">The unique name of the Organisation counter (e.g., "orgId_sequence").</param>
        /// <returns>The next sequence value (integer part) as a long.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown if there's a problem getting or creating the counter.</exception>
        Task<long> GetNextSequenceValue(string sequenceName);
    }

}
