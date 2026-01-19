// PSEUDOCODE / PLAN (detailed):
// 1. Create an import result DTO to summarise counts and errors.
// 2. Define interface `ICsvImportService` with a single async method:
//      Task<ImportResult> ImportFromCsvAsync(IFormFile file, CancellationToken ct = default);
// 3. Implement `CsvImportService`:
//    - Inject IMongoDatabase, IOptions<AWSDocDbSettings>, ILogger<CsvImportService>
//    - Get collections as BsonDocument for Organisations, HeatNetworks, Users (names from settings)
//    - Read CSV from the provided IFormFile using a StreamReader
//    - Expect header with columns: hnId, hnName, hnLocation, organisationId, organisationName, userEmailId
//    - Map header indices to column names; tolerate different ordering; skip unknown columns
//    - For each non-empty data row:
//       a. Extract values and trim
//       b. If hnId or organisationId missing -> record error and skip
//       c. Upsert organisation: use Update with SetOnInsert for org fields, AddToSet for hnIds; IsUpsert = true
//       d. Upsert heat network: replace or update document keyed by hnId; SetOnInsert name/location
//       e. If userEmailId present -> upsert user keyed by emailId + orgId (or emailId alone) with orgId set
//       f. Track counts: organisationsInserted, organisationsUpdated (if AddToSet modified), heatNetworksInserted, usersInserted
//    - Collect any parsing errors into result
//    - Return ImportResult with counts and error details
// 4. Create an API controller `ImportController` with POST action `UploadCsv`:
//    - Route: POST /api/import/upload-csv
//    - Accept IFormFile `file` (multipart/form-data)
//    - Validate file present and size > 0
//    - Call csvImportService.ImportFromCsvAsync(file)
//    - Return 200 OK with ImportResult or 400 on invalid input
//
// Notes:
// - Use simple CSV parsing (split on ','), trim quotes. This is minimal; for production consider CsvHelper.
// - Use BsonDocument collections to avoid tight model coupling.
// - No distributed transaction is used; each upsert is independent.
// - Code is written to be dependency-injectable and testable.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using HNTAS.Core.Api.Configuration;

namespace HNTAS.Core.Api.Controllers
{
    public class ImportResult
    {
        public int RowsProcessed { get; set; }
        public int OrganisationsInserted { get; set; }
        public int OrganisationsUpdated { get; set; }
        public int HeatNetworksInserted { get; set; }
        public int HeatNetworksUpdated { get; set; }
        public int UsersInserted { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    public interface ICsvImportService
    {
        Task<ImportResult> ImportFromCsvAsync(IFormFile file, CancellationToken ct = default);
    }

    public class CsvImportService : ICsvImportService
    {
        private readonly IMongoCollection<BsonDocument> _orgCollection;
        private readonly IMongoCollection<BsonDocument> _heatNetworkCollection;
        private readonly IMongoCollection<BsonDocument> _usersCollection;
        private readonly ILogger<CsvImportService> _logger;

        public CsvImportService(
            IMongoDatabase mongoDatabase,
            IOptions<AWSDocDbSettings> dbSettings,
            ILogger<CsvImportService> logger)
        {
            _logger = logger;
            var settings = dbSettings.Value ?? throw new ArgumentNullException(nameof(dbSettings));
            _orgCollection = mongoDatabase.GetCollection<BsonDocument>(settings.OrganisationsCollectionName);
            _heatNetworkCollection = mongoDatabase.GetCollection<BsonDocument>(settings.HeatNetworksCollectionName);
            _usersCollection = mongoDatabase.GetCollection<BsonDocument>(settings.UsersCollectionName);
        }

        public async Task<ImportResult> ImportFromCsvAsync(IFormFile file, CancellationToken ct = default)
        {
            var result = new ImportResult();

            if (file == null || file.Length == 0)
            {
                result.Errors.Add("No file provided or file is empty.");
                return result;
            }

            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream);

            string? headerLine = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(headerLine))
            {
                result.Errors.Add("CSV is missing a header row.");
                return result;
            }

            // Parse header and map columns
            var headers = SplitCsvLine(headerLine);
            var headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Length; i++)
            {
                headerIndex[headers[i].Trim()] = i;
            }

            // Expected columns (these are the export column names)
            bool hasHnId = headerIndex.ContainsKey("hnId");
            bool hasHnName = headerIndex.ContainsKey("hnName");
            bool hasHnLocation = headerIndex.ContainsKey("hnLocation");
            bool hasOrganisationId = headerIndex.ContainsKey("organisationId");
            bool hasOrganisationName = headerIndex.ContainsKey("organisationName");
            bool hasUserEmailId = headerIndex.ContainsKey("userEmailId");

            if (!hasHnId || !hasOrganisationId)
            {
                result.Errors.Add("CSV must contain at least 'hnId' and 'organisationId' columns.");
                return result;
            }

            int lineNumber = 1;
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                lineNumber++;
                if (string.IsNullOrWhiteSpace(line)) continue;

                var cells = SplitCsvLine(line);
                try
                {
                    string hnId = GetCell(cells, headerIndex, "hnId");
                    string hnName = hasHnName ? GetCell(cells, headerIndex, "hnName") : string.Empty;
                    string hnLocation = hasHnLocation ? GetCell(cells, headerIndex, "hnLocation") : string.Empty;
                    string organisationId = GetCell(cells, headerIndex, "organisationId");
                    string organisationName = hasOrganisationName ? GetCell(cells, headerIndex, "organisationName") : string.Empty;
                    string userEmailId = hasUserEmailId ? GetCell(cells, headerIndex, "userEmailId") : string.Empty;

                    if (string.IsNullOrWhiteSpace(hnId) || string.IsNullOrWhiteSpace(organisationId))
                    {
                        result.Errors.Add($"Line {lineNumber}: missing hnId or organisationId - skipped.");
                        continue;
                    }

                    result.RowsProcessed++;

                    // Upsert organisation: add hnId to hnIds array and set name on insert
                    var orgFilter = Builders<BsonDocument>.Filter.Eq("orgId", organisationId);
                    var orgUpdate = Builders<BsonDocument>.Update
                        .SetOnInsert("orgId", organisationId)
                        .SetOnInsert("name", organisationName ?? string.Empty)
                        .SetOnInsert("createdAt", DateTime.UtcNow)
                        .AddToSet("hnIds", hnId);

                    var orgOptions = new UpdateOptions { IsUpsert = true };
                    var orgUpdateResult = await _orgCollection.UpdateOneAsync(orgFilter, orgUpdate, orgOptions, ct);

                    if (orgUpdateResult.UpsertedId != null)
                        result.OrganisationsInserted++;
                    else if (orgUpdateResult.ModifiedCount > 0)
                        result.OrganisationsUpdated++;

                    // Upsert heat network by hnId
                    var hnFilter = Builders<BsonDocument>.Filter.Eq("hnId", hnId);
                    var hnUpdate = Builders<BsonDocument>.Update
                        .SetOnInsert("hnId", hnId)
                        .SetOnInsert("name", hnName ?? string.Empty)
                        .SetOnInsert("location", hnLocation ?? string.Empty)
                        .SetOnInsert("createdAt", DateTime.UtcNow)
                        // also keep name/location up-to-date if desired - for now only set on insert to avoid overwriting
                        ;

                    var hnOptions = new UpdateOptions { IsUpsert = true };
                    var hnUpdateResult = await _heatNetworkCollection.UpdateOneAsync(hnFilter, hnUpdate, hnOptions, ct);

                    if (hnUpdateResult.UpsertedId != null)
                        result.HeatNetworksInserted++;
                    else if (hnUpdateResult.ModifiedCount > 0)
                        result.HeatNetworksUpdated++;

                    // Upsert user if email provided
                    if (!string.IsNullOrWhiteSpace(userEmailId))
                    {
                        var userFilter = Builders<BsonDocument>.Filter.And(
                            Builders<BsonDocument>.Filter.Eq("emailId", userEmailId),
                            Builders<BsonDocument>.Filter.Eq("orgId", organisationId)
                        );

                        var userUpdate = Builders<BsonDocument>.Update
                            .SetOnInsert("emailId", userEmailId)
                            .SetOnInsert("orgId", organisationId)
                            .SetOnInsert("createdAt", DateTime.UtcNow);

                        var userOptions = new UpdateOptions { IsUpsert = true };
                        var userUpdateResult = await _usersCollection.UpdateOneAsync(userFilter, userUpdate, userOptions, ct);

                        if (userUpdateResult.UpsertedId != null)
                            result.UsersInserted++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing CSV line {LineNumber}", lineNumber);
                    result.Errors.Add($"Line {lineNumber}: {ex.Message}");
                }
            }

            return result;
        }

        // Basic CSV splitter: splits on commas, handles simple quoted fields with " around value and double quotes inside
        private static string[] SplitCsvLine(string line)
        {
            if (line == null) return Array.Empty<string>();
            var values = new List<string>();
            bool inQuotes = false;
            var current = new System.Text.StringBuilder();
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"' )
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        // escaped double quote
                        current.Append('"');
                        i++; // skip next
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(current.ToString().Trim());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            values.Add(current.ToString().Trim());
            return values.ToArray();
        }

        private static string GetCell(string[] cells, Dictionary<string, int> headerIndex, string columnName)
        {
            if (!headerIndex.TryGetValue(columnName, out var idx)) return string.Empty;
            if (idx < 0 || idx >= cells.Length) return string.Empty;
            // Strip optional surrounding quotes
            var raw = cells[idx].Trim();
            if (raw.Length >= 2 && raw[0] == '"' && raw[^1] == '"')
            {
                raw = raw.Substring(1, raw.Length - 2).Replace("\"\"", "\"");
            }
            return raw;
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class ImportController : ControllerBase
    {
        private readonly ICsvImportService _csvImportService;
        private readonly ILogger<ImportController> _logger;

        public ImportController(ICsvImportService csvImportService, ILogger<ImportController> logger)
        {
            _csvImportService = csvImportService;
            _logger = logger;
        }

        /// <summary>
        /// Upload a CSV file and import rows into Organisations, HeatNetworks and Users collections.
        /// Expected CSV headers: hnId, hnName, hnLocation, organisationId, organisationName, userEmailId
        /// Existing route kept for compatibility.
        /// </summary>
        [HttpPost("upload-csv")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadCsv([FromForm] IFormFile file, CancellationToken ct)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No file provided or file is empty." });
            }

            try
            {
                var result = await _csvImportService.ImportFromCsvAsync(file, ct);
                return Ok(result);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("CSV import cancelled by request.");
                return StatusCode(StatusCodes.Status499ClientClosedRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while importing CSV.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }

        /// <summary>
        /// New upload endpoint (alternate route) to accept CSV uploads and import immediately.
        /// Route: POST /api/import/upload
        /// </summary>
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAndImport([FromForm] IFormFile file, CancellationToken ct)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No file provided or file is empty." });
            }

            try
            {
                var result = await _csvImportService.ImportFromCsvAsync(file, ct);
                return Ok(result);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("CSV import cancelled by request.");
                return StatusCode(StatusCodes.Status499ClientClosedRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while importing CSV.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }
    }
}