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
            
            var skipUserAndOrgCheck = false;
            var ofgemDataModelPostImport = new OfgemDataModelPostImport() { HeatNetworkId = new List<string>()};
            var orgId = "";
            string userId = "";
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

                    // STEP 1: Create or fetch user (by emailId + oneLoginId)
                    
                    var orgType = hasCompaniesHouseNo ? Enums.OrganisationType.UkCompaniesHouse : Enums.OrganisationType.OtherUkOrganisation;
                    
                    string userOrgId = "";
                    string userEmailId = "";                    

                    if (!skipUserAndOrgCheck)
                    {
                        User existingUserWithRPRole = await _userService.GetRpAsync();
                        if (existingUserWithRPRole == null)
                        {
                            // Create a minimal user first (RP role)
                            var newUser = new User
                            {
                                EmailId = emailId,
                                OneLoginId = oneLoginId,
                                PreferredContactType = Enums.PreferredContactType.Mobile,
                                LandlineNumber = null,
                                MobileNumber = phoneNumber,
                                Roles = new List<Enums.UserRole>() { Enums.UserRole.ResponsiblePerson },
                                HnRoleMappings = new List<HnRoleMapping>(),
                                Status = Enums.UserStatus.Active,
                                CreatedAt = DateTime.UtcNow
                            };

                            await _userService.CreateAsync(newUser);
                            userId = newUser.Id!;
                            userEmailId = newUser.EmailId;
                            _logger.LogInformation("Created new User {userId}.", userId);
                            result.UsersInserted++;
                            ofgemDataModelPostImport.IsUserExist = false;
                            ofgemDataModelPostImport.UserEmailId = userEmailId;
                            ofgemDataModelPostImport.UserId = userId;
                        }
                        else
                        {
                            userId = existingUserWithRPRole.Id!;
                            userOrgId = existingUserWithRPRole.OrgId!;
                            userEmailId = existingUserWithRPRole.EmailId;
                            _logger.LogInformation("User {userId} already exists.", userId);
                            result.UsersUpdated++;
                            ofgemDataModelPostImport.IsUserExist = true;
                            ofgemDataModelPostImport.UserEmailId = userEmailId;
                            ofgemDataModelPostImport.UserId = userId;
                        }
                        
                        // STEP 2.2: Check if Org is associated with RP, if not then Create Organisation

                        Organisation existingOrg = null;
                        if (orgType == Enums.OrganisationType.UkCompaniesHouse)
                            existingOrg = await _organisationService.GetByCompanyHouseNumberAsync(companiesHouseNo);
                        else if (orgType == Enums.OrganisationType.OtherUkOrganisation)
                            existingOrg = await _organisationService.GetByOrgIdOrNameAsync(organisationName);
                        
                        if (!string.IsNullOrEmpty(userOrgId))
                        {
                            orgId = userOrgId;
                            skipUserAndOrgCheck = true;
                            ofgemDataModelPostImport.IsOrganisationExist = true;
                            ofgemDataModelPostImport.OrganisationId = orgId;
                            _logger.LogInformation("Organisation already associated with user. Skipping creation.");
                        }
                        else if (existingOrg != null)
                        {
                            orgId = existingOrg!.OrgId;
                            ofgemDataModelPostImport.IsOrganisationExist = true;
                            ofgemDataModelPostImport.OrganisationId = orgId;
                            _logger.LogInformation("Organisation already exists. Skipping creation.");
                            await _userService.UpdateOrgIdAsync(userId!, orgId!);
                            _logger.LogInformation("Updated new user {userId}.", userId);
                        }
                        else
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
                            ofgemDataModelPostImport.IsOrganisationExist = false;
                            ofgemDataModelPostImport.OrganisationId = orgId;
                            _logger.LogInformation("Inserted Organisation named {organisationName}.", organisationName);

                            await _userService.UpdateOrgIdAsync(userId!, orgId);
                            _logger.LogInformation("Updated new user {userId}.", userId);                            
                        }
                        if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(orgId))
                            skipUserAndOrgCheck = true;
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
                            UHnId = hnId.Replace("HN", ""),
                            Name = hnName,
                            Address = new RegisteredAddress
                            {
                                AddressLine1 = ecStreetAddress ?? string.Empty,
                                AddressLine2 = null,
                                Town = ecTown ?? string.Empty,
                                County = null,
                                Postcode = ecPostcode ?? string.Empty,
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
                            NetworkElements = null,
                            CreatedBy = userId,
                            CreatedAt = string.IsNullOrWhiteSpace(dateOfHnRegistration) ? DateTime.UtcNow : DateTime.ParseExact(dateOfHnRegistration, new[] { "dd/MM/yyyy", "d/M/yyyy" }, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal)
                        };
                        await _heatNetworkService.CreateAsync(newHn);                        
                        ofgemDataModelPostImport.HeatNetworkId.Add(hnId);
                        result.HeatNetworksInserted++;

                        // check the HeatNetworkId in Organisation collection and update the Organisation hnIds
                        await _organisationService.UpdateAsync(orgId!, hnId);
                    }                    
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing CSV line {Line}", lineNumber);
                    result.Errors.Add($"Line {lineNumber}: {ex.Message}");
                }
            }
            // TODO: SENDING EMAIL
            
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

    public class OfgemDataModelPostImport
    {
        public List<string> HeatNetworkId { get; set; }
        public string OrganisationId { get; set; }
        public string UserId { get; set; }
        public string UserEmailId { get; set; }
        public bool IsUserExist { get; set; }
        public bool IsOrganisationExist { get; set; }
    }
}