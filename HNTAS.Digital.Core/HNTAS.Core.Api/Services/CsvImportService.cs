
using HNTAS.Core.Api.Controllers;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Interfaces;
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
        private readonly IOrganisationService _organisationService;
        private readonly IUserService _userService;
        private readonly IHeatNetworkService _heatNetworkService;
        private readonly ICounterService _orgCounterService;
        private readonly ILogger<CsvImportService> _logger;

        public CsvImportService(
            IMongoDatabase db,
            IOrganisationService organisationService,
            IUserService userService,
            IHeatNetworkService heatNetworkService,
            ICounterService orgCounterService,
            ILogger<CsvImportService> logger)
        {
            _orgCollection = db.GetCollection<BsonDocument>("Organisations");
            _heatNetworkCollection = db.GetCollection<BsonDocument>("HeatNetworks");
            _usersCollection = db.GetCollection<BsonDocument>("Users");
            _organisationService = organisationService;
            _userService = userService;
            _heatNetworkService = heatNetworkService;
            _orgCounterService = orgCounterService;
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
                    #region Reading CSV line
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
                    string dateOfOrgRegistration = GetCell(cells, headerIndex, "DateOfRegistration");
                    string hnId = GetCell(cells, headerIndex, "HnId");
                    string hnName = GetCell(cells, headerIndex, "HnName");
                    string dateOfHnRegistration = GetCell(cells, headerIndex, "DateOfHnRegistration");
                    string registrationSource = GetCell(cells, headerIndex, "RegistrationSource");
                    string ecStreetAddress = GetCell(cells, headerIndex, "EcStreetAddress");
                    string ecTown = GetCell(cells, headerIndex, "EcTown");
                    string ecPostcode = GetCell(cells, headerIndex, "EcPostcode");                  
                    string ecLat = GetCell(cells, headerIndex, "ECLatitude");
                    string ecLong = GetCell(cells, headerIndex, "ECLongitude");

                    #endregion

                    #region Validating mandatory fields
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
                    #endregion

                    // STEP 1: Create or fetch user (by emailId + oneLoginId)
                    User existingUser = await _userService.GetByEmailAsync(emailId);
                    var orgType = hasCompaniesHouseNo ? HNTAS.Core.Api.Enums.OrganisationType.UkCompaniesHouse : HNTAS.Core.Api.Enums.OrganisationType.OtherUkOrganisation;
                    string userId;
                    string userOrgId = "";

                    if (existingUser == null)
                    {
                        // Create a minimal user first
                        var newUser = new User
                        {
                            EmailId = emailId,
                            OneLoginId = oneLoginId,
                            PreferredContactType = HNTAS.Core.Api.Enums.PreferredContactType.Mobile,
                            LandlineNumber = null,
                            MobileNumber = phoneNumber,
                            Roles = new List<HNTAS.Core.Api.Enums.UserRole>(),  // empty for now
                            HnRoleMappings = new List<HnRoleMapping>(),
                            Status = HNTAS.Core.Api.Enums.UserStatus.Active,
                            CreatedAt = DateTime.UtcNow
                        };

                        await _userService.CreateAsync(newUser);                        
                        userId = newUser.Id;
                        _logger.LogInformation("Created new User {userId}.", userId);
                        result.UsersInserted++;
                    }
                    else
                    {
                        userId = existingUser.Id;
                        userOrgId = existingUser.OrgId;
                        _logger.LogInformation("User {userId} already exists.", userId);
                        result.UsersUpdated++;
                    }                    

                    // STEP 2.2: Create Organisation

                    Organisation existingOrg = null;
                    if(orgType == HNTAS.Core.Api.Enums.OrganisationType.UkCompaniesHouse)
                        existingOrg = await _organisationService.GetByIdAsync(companiesHouseNo);
                    else if(orgType == HNTAS.Core.Api.Enums.OrganisationType.OtherUkOrganisation)
                        existingOrg = await _organisationService.GetByOrgIdOrNameAsync(organisationName);
                    var orgId = "";
                    if (existingOrg == null)
                    {
                        Organisation newOrg = new Organisation
                        {
                            Type = orgType,
                            OrgId = $"ORG{await _orgCounterService.GetNextSequenceValue("orgId_sequence"):D7}",
                            CompaniesHouseNumber = hasCompaniesHouseNo ? companiesHouseNo : null,
                            Name = organisationName,
                            RegisteredAddress = new RegisteredAddress
                            {
                                AddressLine1 = orgStreetAddress,
                                AddressLine2 = null,
                                Town = orgTown,
                                County = null,
                                Postcode = orgPostcode,
                                Country = "United Kingdom"
                            },
                            HnIds = new List<string> { hnId },
                            CreatedBy = userId,
                            CreatedAt = string.IsNullOrWhiteSpace(dateOfOrgRegistration) ? DateTime.UtcNow : DateTime.ParseExact(dateOfHnRegistration, new[] { "dd/MM/yyyy", "d/M/yyyy" }, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal)
                        };

                        await _organisationService.CreateAsync(newOrg);
                        orgId = newOrg.OrgId;
                        result.OrganisationsInserted++;
                        _logger.LogInformation("Inserted Organisation named {organisationName}.", organisationName);
                    }
                    else
                    {
                        orgId = existingOrg.OrgId;
                        _logger.LogInformation("Organisation CHN already exists. Skipping.");
                    }

                    if (string.IsNullOrEmpty(userOrgId) && !string.IsNullOrWhiteSpace(orgId))
                    {
                        await _userService.UpdateOrgIdAsync(userId, orgId);
                        _logger.LogInformation("Updated new user {userId}.", userId);
                    }

                    // STEP 3: Create HeatNetwork
                                        
                    var existingHn = await _heatNetworkService.GetByHnIdAsync(hnId);

                    if (existingHn != null)
                    {
                        _logger.LogInformation("HeatNetwork already exists. Skipping insert.");
                    }
                    else
                    {
                        HeatNetwork newHn = new HeatNetwork
                        {
                            HnId = hnId,
                            Name = hnName,
                            Address = new RegisteredAddress
                            {
                                AddressLine1 = ecStreetAddress ?? string.Empty,
                                AddressLine2 = null,
                                Town = ecTown ?? string.Empty,
                                County = null,
                                Postcode =  ecPostcode ?? string.Empty,
                                Country = "United Kingdom"
                            },
                            ECDetails = new ECDetails
                            {
                                Latitude = string.IsNullOrWhiteSpace(ecLat) ? (decimal?)null : decimal.Parse(ecLat, System.Globalization.CultureInfo.InvariantCulture),
                                Longitude = string.IsNullOrWhiteSpace(ecLong) ? (decimal?)null : decimal.Parse(ecLong, System.Globalization.CultureInfo.InvariantCulture)
                            },
                            RegistrationSource = HNTAS.Core.Api.Enums.RegistrationSource.OFGEM,
                            Pathway = null,
                            Soa = null,
                            CreatedBy = userId,
                            CreatedAt = string.IsNullOrWhiteSpace(dateOfHnRegistration) ? DateTime.UtcNow : DateTime.ParseExact(dateOfHnRegistration, new[] { "dd/MM/yyyy", "d/M/yyyy" }, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal)
                        };
                        await _heatNetworkService.CreateAsync(newHn);
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
                        _logger.LogInformation("Appended hnRoleMapping for user.");
                    else
                        _logger.LogInformation("User {EmailId} already has hnRoleMapping for this heat network skipped", emailId, hnId);
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