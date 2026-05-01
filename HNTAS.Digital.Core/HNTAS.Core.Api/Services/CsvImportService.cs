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
        private readonly IMongoCollection<Organisation> _orgCollection;
        private readonly IMongoCollection<HeatNetwork> _heatNetworkCollection;
        private readonly IMongoCollection<User> _usersCollection;
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
            _orgCollection = db.GetCollection<Organisation>("Organisations");
            _heatNetworkCollection = db.GetCollection<HeatNetwork>("HeatNetworks");
            _usersCollection = db.GetCollection<User>("Users");
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

            var ofgemDataModelPostImportList = new List<OfgemDataModelPostImport>();
            var newRp = string.Empty;
            var newOrgs = new List<string>();
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
                    bool hasCompaniesHouseNo = !string.IsNullOrWhiteSpace(companiesHouseNo);
                    bool hasOrgAddress =
                        !string.IsNullOrWhiteSpace(organisationName) &&
                        !string.IsNullOrWhiteSpace(orgStreetAddress) &&
                        !string.IsNullOrWhiteSpace(orgPostcode);
                    bool missingOrganisation = !(hasCompaniesHouseNo || hasOrgAddress);

                    if (missingIds ||
                        missingOrganisation ||
                        string.IsNullOrWhiteSpace(hnId))
                    {
                        result.Errors.Add($"Line {lineNumber}: Missing required fields.");
                        continue;
                    }

                    result.RowsProcessed++;
                    #endregion

                    var ofgemDataModelPostImport = new OfgemDataModelPostImport
                    {                        
                        OrganisationId = "",
                        UserEmailId = "",
                        //IsOrganisationExist = false, // to be updated later
                        //IsUserExist = false // to be updated later
                    };

                    // STEP 1: Create or fetch user (by emailId + oneLoginId)
                    User existingUserWithRPRole = await _userService.GetRpAsync();
                    var orgType = hasCompaniesHouseNo ? Enums.OrganisationType.UkCompaniesHouse : Enums.OrganisationType.OtherUkOrganisation;
                    string userId;
                    string userOrgId = "";
                    string userEmailId = "";

                    if (existingUserWithRPRole == null)
                    {
                        // Create a minimal user first
                        var newUser = new User
                        {
                            EmailId = emailId,
                            OneLoginId = oneLoginId,
                            PreferredContactType = Enums.PreferredContactType.Mobile,
                            LandlineNumber = null,
                            MobileNumber = phoneNumber,
                            Roles = new List<Enums.UserRole>(),  // empty for now
                            HnRoleMappings = new List<HnRoleMapping>(),
                            Status = Enums.UserStatus.Active,
                            CreatedAt = DateTime.UtcNow
                        };

                        await _userService.CreateAsync(newUser);                        
                        userId = newUser.Id;
                        userEmailId = newUser.EmailId;
                        _logger.LogInformation("Created new User {userId}.", userId);
                        result.UsersInserted++;
                        newRp = userEmailId;
                    }
                    else
                    {
                        userId = existingUserWithRPRole.Id;
                        userOrgId = existingUserWithRPRole.OrgId;
                        userEmailId = existingUserWithRPRole.EmailId;
                        _logger.LogInformation("User {userId} already exists.", userId);
                        result.UsersUpdated++;
                        //ofgemDataModelPostImport.IsUserExist = true;
                    }
                    ofgemDataModelPostImport.UserId = userId;
                    ofgemDataModelPostImport.UserEmailId = userEmailId;
                    // STEP 2.2: Create Organisation

                    Organisation existingOrg = null;
                    if(orgType == Enums.OrganisationType.UkCompaniesHouse)
                        existingOrg = await _organisationService.GetByCompanyHouseNumberAsync(companiesHouseNo);
                    else if(orgType == Enums.OrganisationType.OtherUkOrganisation)
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
                        newOrgs.Add(orgId);
                        _logger.LogInformation("Inserted Organisation named {organisationName}.", organisationName);
                    }
                    else
                    {
                        orgId = existingOrg.OrgId;
                        //ofgemDataModelPostImport.IsOrganisationExist = true;
                        _logger.LogInformation("Organisation CHN already exists. Skipping.");
                    }

                    ofgemDataModelPostImport.OrganisationId = orgId;

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
                            RegistrationSource = Enums.RegistrationSource.OFGEM,
                            Pathway = null,
                            Soa = null,
                            CreatedBy = userId,
                            CreatedAt = string.IsNullOrWhiteSpace(dateOfHnRegistration) ? DateTime.UtcNow : DateTime.ParseExact(dateOfHnRegistration, new[] { "dd/MM/yyyy", "d/M/yyyy" }, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal)
                        };
                        await _heatNetworkService.CreateAsync(newHn);
                        ofgemDataModelPostImport.HeatNetworkId = newHn.HnId;
                        result.HeatNetworksInserted++;
                    }

                    // STEP 4: Update User with HN + Role mappings

                    //var filter = Builders<User>.Filter.And(
                    //    Builders<User>.Filter.Eq("emailId", userEmailId),
                    //    Builders<User>.Filter.Not(
                    //        Builders<User>.Filter.ElemMatch("hnRoleMappings",
                    //            Builders<User>.Filter.Eq("hnId", hnId)
                    //        )
                    //    )
                    //);

                    //var update = Builders<User>.Update
                    //    .Push("hnRoleMappings", new BsonDocument
                    //    {
                    //        { "hnId", hnId },
                    //        { "role", "ResponsiblePerson" }
                    //    })
                    //    // optional: keep hnIds and roles aligned (also append-only)
                    //    .AddToSet("roles", "ResponsiblePerson");

                    //var res = await _usersCollection.UpdateOneAsync(filter, update, cancellationToken: ct);

                    //if (res.ModifiedCount > 0)
                    //    _logger.LogInformation("Appended hnRoleMapping for user.");
                    //else
                    //    _logger.LogInformation("User {userId} already has hnRoleMapping for this heat network skipped", userId, hnId);

                    ofgemDataModelPostImportList.Add(ofgemDataModelPostImport);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing CSV line {Line}", lineNumber);
                    result.Errors.Add($"Line {lineNumber}: {ex.Message}");
                }
            }
            // TODO: We may need to store the ofgemDataModelPostImportList in a temporary collection for the post-import email step to consume, or (pass it directly - currently doing)
            GetUploadedHeatNetworkData(ofgemDataModelPostImportList, newRp, newOrgs);
            return result;
        }


        public async Task GetUploadedHeatNetworkData(List<OfgemDataModelPostImport> ofgemDataList, string newRp, List<string> newOrgs)
        {
            // iterate through the list and group heatnetwork id by organisation and set the flag to true if user or organisation exist and save to the OfgemDataModelPostImportGroupByOrganisation
                var groupedByOrg = ofgemDataList
                    .GroupBy(x => x.OrganisationId)
                    .Select(g => new OfgemDataModelPostImportGroupByOrganisation
                    {
                        OrganisationId = g.Key,
                        HeatNetworkId = g.Distinct().Where(a => a.HeatNetworkId != null).Select(x => x.HeatNetworkId).ToList(),
                        IsOrganisationExist = !newOrgs.Contains(g.Key),
                        UserId = g.FirstOrDefault()?.UserId ?? string.Empty,
                        UserEmailId = g.FirstOrDefault()?.UserEmailId ?? string.Empty
                    })
                    .ToList();

            var groupByUserEmailId = ofgemDataList
                    .GroupBy(x => x.UserEmailId)
                    .Select(g => new OfgemDataModelPostImportGroupByOrganisation
                    {
                        UserEmailId = g.Key,
                        HeatNetworkId = g.Distinct().Where(a => a.HeatNetworkId != null).Select(x => x.HeatNetworkId).ToList(),
                        IsUserExist = g.FirstOrDefault(x => x.UserEmailId == newRp) != null,
                        OrganisationId = g.FirstOrDefault()?.OrganisationId ?? string.Empty,
                        UserId = g.FirstOrDefault()?.UserId ?? string.Empty
                    })
                    .ToList();
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

    public class OfgemDataModelPostImportGroupByOrganisation
    {
        public string OrganisationId { get; set; }
        public string UserId { get; set; }
        public string UserEmailId { get; set; }
        public List<string> HeatNetworkId { get; set; }
        public bool IsOrganisationExist { get; set; }
        public bool IsUserExist { get; set; }
    }

    public class OfgemDataModelPostImport
    {
        public string HeatNetworkId { get; set; }
        public string OrganisationId { get; set; }
        public string UserId { get; set; }
        public string UserEmailId { get; set; }
        //public bool IsUserExist { get; set; }
        //public bool IsOrganisationExist { get; set; }
    }

    public enum OfgemGroupType
    {
        Organisation,
        RegulatoryContactEmail
    }
}