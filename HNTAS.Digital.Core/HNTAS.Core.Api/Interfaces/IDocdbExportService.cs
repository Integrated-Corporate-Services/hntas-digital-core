
using MongoDB.Bson;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HNTAS.Core.Api.Services
{
    public interface IDocdbExportService
    {
        string Elem<T>(string property);
        /// <summary>
        /// Returns joined Users → HeatNetworks → Organisations rows.
        /// Use <paramref name="take"/> to limit for testing.
        /// </summary>
        //Task<List<DocdbExportRow>> GetAsync(int? take = null);
        Task<List<DocdbExportRow>> GetFlattenedHeatNetworkUserOrgAsync();
    }
}
