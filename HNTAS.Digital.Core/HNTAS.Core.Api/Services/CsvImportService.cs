
using CsvHelper;
using CsvHelper.Configuration;
using HNTAS.Core.Api.Controllers;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Globalization;
using System.Text;


namespace HNTAS.Core.Api.Services
{
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
            IMongoDatabase db,
            ILogger<CsvImportService> logger)
        {
            _orgCollection = db.GetCollection<BsonDocument>("Organisations");
            _heatNetworkCollection = db.GetCollection<BsonDocument>("HeatNetworks");
            _usersCollection = db.GetCollection<BsonDocument>("Users");
            _logger = logger;
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

            var headers = SplitCsvLine(headerLine);
            var headerIndex = headers
                .Select((h, i) => new { h, i })
                .ToDictionary(x => x.h.Trim(), x => x.i, StringComparer.OrdinalIgnoreCase);

            int lineNumber = 1;
            string? line;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                lineNumber++;
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var cells = SplitCsvLine(line);

                    // Extract CSV fields
                    string emailId = GetCell(cells, headerIndex, "EmailId");
                    string oneLoginId = GetCell(cells, headerIndex, "OneLoginId");
                    string organisationName = GetCell(cells, headerIndex, "OrganisationName");
                    string orgStreetAddress = GetCell(cells, headerIndex, "OrgStreetAddress");
                    string orgTown = GetCell(cells, headerIndex, "OrgTown");
                    string orgPostcode = GetCell(cells, headerIndex, "OrgPostcode");
                    string phoneNumber = GetCell(cells, headerIndex, "PhoneNumber");
                    string companiesHouseNo = GetCell(cells, headerIndex, "CompaniesHouseNo");
                    string hnId = GetCell(cells, headerIndex, "HnId");
                    string hnName = GetCell(cells, headerIndex, "HnName");
                    string ecLat = GetCell(cells, headerIndex, "ECLatitude");
                    string ecLong = GetCell(cells, headerIndex, "ECLongitude");

                    // Validate mandatory fields
                    if (string.IsNullOrWhiteSpace(emailId) ||
                        string.IsNullOrWhiteSpace(companiesHouseNo) ||
                        string.IsNullOrWhiteSpace(hnId))
                    {
                        result.Errors.Add($"Line {lineNumber}: Missing required fields.");
                        continue;
                    }

                    result.RowsProcessed++;


                    // STEP 1: Create or fetch user (by emailId + oneLoginId)
                    var userFilter = Builders<BsonDocument>.Filter.Eq("emailId", emailId);
                    var existingUser = await _usersCollection.Find(userFilter).FirstOrDefaultAsync(ct);
                    string userId;
                    string userOrgId = "";

                    if (existingUser == null)
                    {
                        // Create a minimal user first
                        var newUser = new BsonDocument
                        {
                            { "emailId", emailId },
                            { "oneloginId", oneLoginId },
                            { "preferredContactType", "Mobile" },
                            { "landlineNumber", BsonNull.Value },
                            { "mobileNumber", phoneNumber },
                            { "roles", new BsonArray() },  // empty for now
                            { "hnRoleMappings", new BsonArray() },
                            { "status", "Active" },
                            { "createdAt", DateTime.UtcNow }
                        };

                        await _usersCollection.InsertOneAsync(newUser, cancellationToken: ct);
                        userId = newUser["_id"].AsObjectId.ToString();

                        _logger.LogInformation("Created new User {EmailId} with _id {UserId}", emailId, userId);
                        result.UsersInserted++;
                    }
                    else
                    {
                        userId = existingUser["_id"].AsObjectId.ToString();
                        userOrgId = existingUser.TryGetValue("orgId", out var x) ? x.AsString : null;
                        _logger.LogInformation("User {EmailId} already exists with _id {UserId}", emailId, userId);
                        result.UsersUpdated++;
                    }


                    // STEP 2: Create Organisation
                    var orgFilter = Builders<BsonDocument>.Filter.Eq("companiesHouseNumber", companiesHouseNo);
                    var existingOrg = await _orgCollection.Find(orgFilter).FirstOrDefaultAsync(ct);
                    var orgId = "";

                    if (existingOrg == null)
                    {
                        var orgDoc = new BsonDocument
                        {
                            { "orgId", Guid.NewGuid().ToString() },
                            { "type", "UkCompaniesHouse" },
                            { "companiesHouseNumber", companiesHouseNo },
                            { "name", organisationName },
                            { "registeredAddress", new BsonDocument
                                {
                                    { "addressLine1", orgStreetAddress },
                                    { "addressLine2", "" },
                                    { "town", orgTown },
                                    { "county", BsonNull.Value },
                                    { "postcode", orgPostcode },
                                    { "country", "United Kingdom" }
                                }
                            },
                            { "hnIds", new BsonArray { hnId } },
                            { "createdBy", userId },   // IMPORTANT
                            { "createdAt", DateTime.UtcNow }
                        };

                        await _orgCollection.InsertOneAsync(orgDoc, cancellationToken: ct);
                        result.OrganisationsInserted++;
                        _logger.LogInformation("Inserted Organisation CHN {CHN} with createdBy {UserId}", companiesHouseNo, userId);
                    }
                    else
                    {
                        orgId = existingOrg.TryGetValue("orgId", out var v) ? v.AsString : null;
                        _logger.LogInformation("Organisation CHN {CHN} already exists. Skipping.", companiesHouseNo);
                    }


                    // STEP 3: Create HeatNetwork
                    var hnFilter = Builders<BsonDocument>.Filter.Eq("hnId", hnId);
                    var existingHn = await _heatNetworkCollection.Find(hnFilter).FirstOrDefaultAsync(ct);

                    if (existingHn == null)
                    {
                        var hnDoc = new BsonDocument
                        {
                            { "hnId", hnId },
                            { "name", hnName },
                            { "location", $"{ecLat},{ecLong}" },
                            { "pathway", BsonNull.Value },
                            { "soa", BsonNull.Value },
                            { "createdBy", userId },  // IMPORTANT
                            { "createdAt", DateTime.UtcNow }
                        };

                        await _heatNetworkCollection.InsertOneAsync(hnDoc, cancellationToken: ct);
                        result.HeatNetworksInserted++;
                        _logger.LogInformation("Inserted HeatNetwork {HnId} with createdBy {UserId}", hnId, userId);
                    }
                    else
                    {
                        _logger.LogInformation("HeatNetwork {HnId} already exists. Skipping.", hnId);
                    }

                    // STEP 4: Update User with HN + Role mappings

                    var filter = Builders<BsonDocument>.Filter.And(
                        Builders<BsonDocument>.Filter.Eq("emailId", emailId),
                        Builders<BsonDocument>.Filter.Not(
                            Builders<BsonDocument>.Filter.ElemMatch("hnRoleMappings",
                                Builders<BsonDocument>.Filter.Eq("hnId", hnId)
                            )
                        )
                    );

                    var update = Builders<BsonDocument>.Update
                        .Push("hnRoleMappings", new BsonDocument
                        {
                            { "hnId", hnId },
                            { "role", "ResponsiblePerson" }
                        })
                        // optional: keep hnIds and roles aligned (also append-only)
                        .AddToSet("roles", "ResponsiblePerson");

                    var res = await _usersCollection.UpdateOneAsync(filter, update, cancellationToken: ct);

                    if (res.ModifiedCount > 0)
                        _logger.LogInformation("Appended hnRoleMapping for user {EmailId}: {HnId}", emailId, hnId);
                    else
                        _logger.LogInformation("User {EmailId} already has hnRoleMapping for {HnId} - skipped", emailId, hnId);
                    
                    if (string.IsNullOrEmpty(userOrgId) && !string.IsNullOrWhiteSpace(orgId))
                    {
                        var updateUserWithOrg = Builders<BsonDocument>.Update
                            .Set("orgId", orgId);

                        await _usersCollection.UpdateOneAsync(
                            Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(userId)),
                            updateUserWithOrg,
                            cancellationToken: ct
                        );

                        _logger.LogInformation("Updated new user {EmailId} with orgId {OrgId}", emailId, orgId);
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing CSV line {Line}", lineNumber);
                    result.Errors.Add($"Line {lineNumber}: {ex.Message}");
                }
            }

            return result;
        }

        // ------------------------------------------------------------
        // Helper CSV utilities
        // ------------------------------------------------------------
        private string[] SplitCsvLine(string line)
        {
            var values = new List<string>();
            bool inQuotes = false;
            var sb = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"' && (i == 0 || line[i - 1] != '\\'))
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (c == ',' && !inQuotes)
                {
                    values.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }

            values.Add(sb.ToString()); // last value
            return values.ToArray();
        }


        private string GetCell(string[] cells, Dictionary<string, int> headerIndex, string col)
        {
            return headerIndex.TryGetValue(col, out int index) && index < cells.Length
                ? cells[index]
                : string.Empty;
        }
    }
}
