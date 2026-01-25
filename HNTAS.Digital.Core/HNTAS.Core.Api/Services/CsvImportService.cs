
using HNTAS.Core.Api.Controllers;
using MongoDB.Bson;
using MongoDB.Driver;
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


        private static BsonValue ToDecimal128OrNull(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return BsonNull.Value;

            // Use invariant culture to avoid comma vs dot decimal issues.
            if (decimal.TryParse(value, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var dec))
            {
                return new BsonDecimal128(dec);
            }

            return BsonNull.Value;
        }

        private static BsonValue ToDateOrNull(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return BsonNull.Value;

            // Your CSV looks like dd/MM/yyyy e.g. 23/12/2025
            if (DateTime.TryParseExact(value.Trim(),
                    new[] { "dd/MM/yyyy", "d/M/yyyy" },
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AssumeUniversal, out var dt))
            {
                return dt;
            }

            // fallback parse if formats vary
            if (DateTime.TryParse(value, out dt))
                return dt;

            return BsonNull.Value;
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
                    string dateOfHnRegistration = GetCell(cells, headerIndex, "DateOfHnRegistration"); //hn
                    string registeredVia = GetCell(cells, headerIndex, "RegisteredVia"); //hn
                    string ecStreetAddress = GetCell(cells, headerIndex, "EcStreetAddress"); // hn
                    string ecTown = GetCell(cells, headerIndex, "EcTown"); // hn
                    string ecPostcode = GetCell(cells, headerIndex, "EcPostcode"); // hn
                    string hnId = GetCell(cells, headerIndex, "HnId");
                    string hnName = GetCell(cells, headerIndex, "HnName");
                    string ecLat = GetCell(cells, headerIndex, "ECLatitude");
                    string ecLong = GetCell(cells, headerIndex, "ECLongitude");


                    bool missingIds =
                        string.IsNullOrWhiteSpace(emailId) ||
                        string.IsNullOrWhiteSpace(oneLoginId) ||
                        string.IsNullOrWhiteSpace(hnId);

                    // Has a CompaniesHouse number
                    bool hasCompaniesHouseNo = !string.IsNullOrWhiteSpace(companiesHouseNo);

                    // No CompaniesHouse number, but has OrgName + StreetAddress + Postcode
                    bool hasOrgAddress =
                        !string.IsNullOrWhiteSpace(organisationName) &&
                        !string.IsNullOrWhiteSpace(orgStreetAddress) &&
                        !string.IsNullOrWhiteSpace(orgPostcode);

                    // Must have either A or B
                    bool missingOrganisation = !(hasCompaniesHouseNo || hasOrgAddress);


                    // Validate mandatory fields
                    if (missingIds ||
                        missingOrganisation ||
                        string.IsNullOrWhiteSpace(hnId))
                    {
                        result.Errors.Add($"Line {lineNumber}: Missing required fields.");
                        continue;
                    }

                    result.RowsProcessed++;
                    var orgType = hasCompaniesHouseNo ? "UkCompaniesHouse" : "OtherUkOrganisation";


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
                            { "type", orgType },
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
                        _logger.LogInformation("Inserted Organisation named {organisationName} with createdBy {UserId}", organisationName, userId);
                    }
                    else
                    {
                        // companiesHouseNo cannot be same, otherUkOrganisations may have same names with different address, so not checking
                        if (orgType == "UkCompaniesHouse")
                        {
                            orgId = existingOrg.TryGetValue("orgId", out var v) ? v.AsString : null;
                            _logger.LogInformation("Organisation CHN {CHN} already exists. Skipping.", companiesHouseNo);
                        }
                        
                    }


                    // STEP 3: Create HeatNetwork

                    var hnFilter = Builders<BsonDocument>.Filter.Eq("hnId", hnId);
                    var existingHn = await _heatNetworkCollection.Find(hnFilter).FirstOrDefaultAsync(ct);

                    if (existingHn != null)
                    {
                        _logger.LogInformation("HeatNetwork {HnId} already exists. Skipping insert.", hnId);
                    }
                    else
                    {
                        _logger.LogInformation("Inserting new HeatNetwork {HnId}", hnId);

                        var hnDoc = new BsonDocument
                        {
                            { "hnId", hnId },
                            { "name", hnName },

                            // NEW: address block from EC* fields
                            { "address", new BsonDocument
                                {
                                    { "addressLine1", ecStreetAddress ?? string.Empty },
                                    { "addressLine2", BsonNull.Value },
                                    { "town", ecTown ?? string.Empty },
                                    { "county", BsonNull.Value },
                                    { "postcode", ecPostcode ?? string.Empty },
                                    { "country", "United Kingdom" }
                                }
                            },
                            { "ecDetails", new BsonDocument
                                {
                                    { "latitude", ToDecimal128OrNull(ecLat) },
                                    { "longitude", ToDecimal128OrNull(ecLong) }
                                }
                            },
                            { "registeredVia", registeredVia ?? string.Empty },        // NEW
                            { "dateOfRegistration", ToDateOrNull(dateOfHnRegistration) },
                            { "pathway", BsonNull.Value },
                            { "soa", BsonNull.Value },
                            { "createdBy", userId },
                            { "createdAt", DateTime.UtcNow }
                        };

                        await _heatNetworkCollection.InsertOneAsync(hnDoc, cancellationToken: ct);
                        result.HeatNetworksInserted++;
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
