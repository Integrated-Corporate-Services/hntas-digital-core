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
        private readonly IOrganisationService _organisationService;
        private readonly IUserService _userService;
        private readonly IHeatNetworkService _heatNetworkService;
        private readonly ICounterService _orgCounterService;
        private readonly ILogger<CsvImportService> _logger;

        public CsvImportService(
            IOrganisationService organisationService,
            IUserService userService,
            IHeatNetworkService heatNetworkService,
            ICounterService orgCounterService,
            ILogger<CsvImportService> logger)
        {
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

            var csvParser = new CsvParser();
            using var stream = file.OpenReadStream();

            if (!csvParser.TryParseHeaders(stream, out var headerIndex, out var error))
            {
                result.Errors.Add(error);
                return result;
            }

            var importContext = new ImportContext();
            int lineNumber = 1;

            await foreach (var line in csvParser.ReadLinesAsync(stream, ct))
            {
                lineNumber++;
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    await ProcessCsvLineAsync(line, headerIndex, lineNumber, importContext, result, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing CSV line {Line}", lineNumber);
                    result.Errors.Add($"Line {lineNumber}: {ex.Message}");
                }
            }

            return result;
        }

        private async Task ProcessCsvLineAsync(
            string line,
            Dictionary<string, int> headerIndex,
            int lineNumber,
            ImportContext context,
            ImportResult result,
            CancellationToken ct)
        {
            var row = ParseCsvRow(line, headerIndex);

            if (!ValidateRow(row, lineNumber, result))
            {
                return;
            }

            result.RowsProcessed++;

            // Process User and Organisation (only once per import)
            if (!context.IsInitialized)
            {
                await InitializeUserAndOrganisationAsync(row, context, result, ct);
            }

            // Process Heat Network (for each row)
            await ProcessHeatNetworkAsync(row, context, result, ct);
        }

        private CsvRow ParseCsvRow(string line, Dictionary<string, int> headerIndex)
        {
            var cells = CsvParser.SplitCsvLine(line);

            return new CsvRow
            {
                EmailId = CsvParser.GetCell(cells, headerIndex, "EmailId"),
                OneLoginId = CsvParser.GetCell(cells, headerIndex, "OneLoginId"),
                OrganisationName = CsvParser.GetCell(cells, headerIndex, "OrganisationName"),
                OrgStreetAddress = CsvParser.GetCell(cells, headerIndex, "OrgStreetAddress"),
                OrgTown = CsvParser.GetCell(cells, headerIndex, "OrgTown"),
                OrgPostcode = CsvParser.GetCell(cells, headerIndex, "OrgPostcode"),
                PhoneNumber = CsvParser.GetCell(cells, headerIndex, "PhoneNumber"),
                CompaniesHouseNo = CsvParser.GetCell(cells, headerIndex, "CompaniesHouseNo"),
                DateOfOrgRegistration = CsvParser.GetCell(cells, headerIndex, "DateOfRegistration"),
                HnId = CsvParser.GetCell(cells, headerIndex, "HnId"),
                HnName = CsvParser.GetCell(cells, headerIndex, "HnName"),
                DateOfHnRegistration = CsvParser.GetCell(cells, headerIndex, "DateOfHnRegistration"),
                EcStreetAddress = CsvParser.GetCell(cells, headerIndex, "EcStreetAddress"),
                EcTown = CsvParser.GetCell(cells, headerIndex, "EcTown"),
                EcPostcode = CsvParser.GetCell(cells, headerIndex, "EcPostcode"),
                EcLatitude = CsvParser.GetCell(cells, headerIndex, "ECLatitude"),
                EcLongitude = CsvParser.GetCell(cells, headerIndex, "ECLongitude")
            };
        }

        private bool ValidateRow(CsvRow row, int lineNumber, ImportResult result)
        {
            var missingFields = new List<string>();

            if (string.IsNullOrWhiteSpace(row.EmailId)) missingFields.Add("EmailId");
            if (string.IsNullOrWhiteSpace(row.OneLoginId)) missingFields.Add("OneLoginId");
            if (string.IsNullOrWhiteSpace(row.HnId)) missingFields.Add("HnId");

            //var hasOrgIdentifier = !string.IsNullOrWhiteSpace(row.CompaniesHouseNo) ||
            //                     (!string.IsNullOrWhiteSpace(row.OrganisationName) &&
            //                      !string.IsNullOrWhiteSpace(row.OrgStreetAddress) &&
            //                      !string.IsNullOrWhiteSpace(row.OrgPostcode));
            var hasOrgIdentifier = !string.IsNullOrWhiteSpace(row.CompaniesHouseNo);
            if (!hasOrgIdentifier)
            {
                missingFields.Add("Organisation details (CompaniesHouseNo or Name+Address+Postcode)");
            }

            if (missingFields.Any())
            {
                result.Errors.Add($"Line {lineNumber}: Missing required fields: {string.Join(", ", missingFields)}");
                return false;
            }

            return true;
        }

        private async Task InitializeUserAndOrganisationAsync(
            CsvRow row,
            ImportContext context,
            ImportResult result,
            CancellationToken ct)
        {
            // Process User
            var existingRpUser = await _userService.GetRpAsync();

            if (existingRpUser == null)
            {
                context.User = await CreateUserAsync(row, result);
            }
            else
            {
                context.User = existingRpUser;
                context.PostImportData.IsUserExist = true;
                result.UsersUpdated++;
                _logger.LogInformation("User {UserId} already exists.", context.UserId);
            }

            context.PostImportData.UserId = context.UserId;
            context.PostImportData.UserEmailId = context.User.EmailId;

            // Process Organisation
            var orgType = !string.IsNullOrWhiteSpace(row.CompaniesHouseNo)
                ? Enums.OrganisationType.UkCompaniesHouse
                : Enums.OrganisationType.OtherUkOrganisation;

            Organisation? orgAssociatedWithUser = null;
            if (!string.IsNullOrEmpty(context.User.OrgId))
            {
                // find if the associated OrgId is based on CompanyHouseNumber
                orgAssociatedWithUser = await _organisationService.GetByOrgIdAsync(context.User.OrgId);
            }
            if (!string.IsNullOrEmpty(orgAssociatedWithUser?.CompaniesHouseNumber))
            {
                context.OrganisationId = context.User.OrgId!;
                context.PostImportData.IsOrganisationExist = true;
                _logger.LogInformation("Organisation already associated with user.");
            }
            else
            {
                //var existingOrg = await FindExistingOrganisationAsync(row, orgType);

                var existingOrg = await FindExistingOrganisationAsync(row);

                if (existingOrg != null)
                {
                    context.OrganisationId = existingOrg.OrgId;
                    context.PostImportData.IsOrganisationExist = true;
                    context.User.OrgId = context.OrganisationId;
                    await _userService.UpdateOrgIdAsync(context.UserId, context.OrganisationId);
                    _logger.LogInformation("Organisation already exists. Linked to user.");
                }
                else
                {
                    context.OrganisationId = await CreateOrganisationAsync(row, orgType, context.UserId, result);
                    context.User.OrgId = context.OrganisationId;
                    await _userService.UpdateOrgIdAsync(context.UserId, context.OrganisationId);
                    _logger.LogInformation("Created new organisation and linked to user.");
                }
            }

            context.PostImportData.OrganisationId = context.OrganisationId;
            context.IsInitialized = true;
        }

        private async Task<User> CreateUserAsync(CsvRow row, ImportResult result)
        {
            var newUser = new User
            {
                EmailId = row.EmailId,
                OneLoginId = row.OneLoginId,
                PreferredContactType = Enums.PreferredContactType.Mobile,
                MobileNumber = row.PhoneNumber,
                Roles = new List<Enums.UserRole> { Enums.UserRole.ResponsiblePerson },
                HnRoleMappings = new List<HnRoleMapping>(),
                Status = Enums.UserStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            await _userService.CreateAsync(newUser);
            result.UsersInserted++;
            _logger.LogInformation("Created new User {UserId}.", newUser.Id);

            return newUser;
        }

        //private async Task<Organisation?> FindExistingOrganisationAsync(CsvRow row, Enums.OrganisationType orgType)
        //{
        //    return orgType == Enums.OrganisationType.UkCompaniesHouse
        //        ? await _organisationService.GetByCompanyHouseNumberAsync(row.CompaniesHouseNo)
        //        : await _organisationService.GetByOrgIdOrNameAsync(row.OrganisationName);
        //}

        private async Task<Organisation?> FindExistingOrganisationAsync(CsvRow row)
        {
            return await _organisationService.GetByCompanyHouseNumberAsync(row.CompaniesHouseNo);
        }

        private async Task<string> CreateOrganisationAsync(
            CsvRow row,
            Enums.OrganisationType orgType,
            string userId,
            ImportResult result)
        {
            var newOrg = new Organisation
            {
                Type = orgType,
                OrgId = $"ORG{await _orgCounterService.GetNextSequenceValue("orgId_sequence"):D7}",
                CompaniesHouseNumber = orgType == Enums.OrganisationType.UkCompaniesHouse ? row.CompaniesHouseNo : null,
                Name = row.OrganisationName,
                RegisteredAddress = new RegisteredAddress
                {
                    AddressLine1 = row.OrgStreetAddress,
                    Town = row.OrgTown,
                    Postcode = row.OrgPostcode,
                    Country = "United Kingdom"
                },
                HnIds = new List<string>(),
                CreatedBy = userId,
                CreatedAt = ParseDate(row.DateOfOrgRegistration)
            };

            await _organisationService.CreateAsync(newOrg);
            result.OrganisationsInserted++;
            _logger.LogInformation("Created Organisation {OrgName}.", newOrg.Name);

            return newOrg.OrgId;
        }

        private async Task ProcessHeatNetworkAsync(
            CsvRow row,
            ImportContext context,
            ImportResult result,
            CancellationToken ct)
        {
            var existingHn = await _heatNetworkService.GetByHnIdAsync(row.HnId);

            if (existingHn != null)
            {
                _logger.LogInformation("HeatNetwork {HnId} already exists.", row.HnId);
                return;
            }

            var newHn = new HeatNetwork
            {
                HnId = row.HnId,
                UHnId = row.HnId.Replace("HN", ""),
                Name = row.HnName,
                Address = new RegisteredAddress
                {
                    AddressLine1 = row.EcStreetAddress ?? string.Empty,
                    Town = row.EcTown ?? string.Empty,
                    Postcode = row.EcPostcode ?? string.Empty,
                    Country = "United Kingdom"
                },
                ECDetails = new ECDetails
                {
                    Latitude = ParseDecimal(row.EcLatitude),
                    Longitude = ParseDecimal(row.EcLongitude)
                },
                RegistrationSource = Enums.RegistrationSource.OFGEM,
                CreatedBy = context.UserId,
                CreatedAt = ParseDate(row.DateOfHnRegistration)
            };

            await _heatNetworkService.CreateAsync(newHn);
            await _organisationService.UpdateAsync(context.OrganisationId, row.HnId);

            context.PostImportData.HeatNetworkId.Add(row.HnId);
            result.HeatNetworksInserted++;
            _logger.LogInformation("Created HeatNetwork {HnId}.", row.HnId);
        }

        private static DateTime ParseDate(string dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString))
                return DateTime.UtcNow;

            return DateTime.ParseExact(
                dateString,
                new[] { "dd/MM/yyyy", "d/M/yyyy" },
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal);
        }

        private static decimal? ParseDecimal(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    // Supporting classes
    internal class CsvRow
    {
        public string EmailId { get; set; } = string.Empty;
        public string OneLoginId { get; set; } = string.Empty;
        public string OrganisationName { get; set; } = string.Empty;
        public string OrgStreetAddress { get; set; } = string.Empty;
        public string OrgTown { get; set; } = string.Empty;
        public string OrgPostcode { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string CompaniesHouseNo { get; set; } = string.Empty;
        public string DateOfOrgRegistration { get; set; } = string.Empty;
        public string HnId { get; set; } = string.Empty;
        public string HnName { get; set; } = string.Empty;
        public string DateOfHnRegistration { get; set; } = string.Empty;
        public string EcStreetAddress { get; set; } = string.Empty;
        public string EcTown { get; set; } = string.Empty;
        public string EcPostcode { get; set; } = string.Empty;
        public string EcLatitude { get; set; } = string.Empty;
        public string EcLongitude { get; set; } = string.Empty;
    }

    internal class ImportContext
    {
        public User? User { get; set; }
        public string UserId => User?.Id ?? string.Empty;
        public string OrganisationId { get; set; } = string.Empty;
        public bool IsInitialized { get; set; }
        public OfgemDataModelPostImport PostImportData { get; set; } = new();
    }

    internal class CsvParser
    {
        public bool TryParseHeaders(Stream stream, out Dictionary<string, int> headerIndex, out string error)
        {
            headerIndex = new Dictionary<string, int>();
            error = string.Empty;

            using var reader = new StreamReader(stream, leaveOpen: true);
            var headerLine = reader.ReadLine();

            if (string.IsNullOrWhiteSpace(headerLine))
            {
                error = "CSV is missing a header row.";
                return false;
            }

            var headers = SplitCsvLine(headerLine);
            headerIndex = headers
                .Select((h, i) => new { h, i })
                .ToDictionary(x => x.h.Trim(), x => x.i, StringComparer.OrdinalIgnoreCase);

            stream.Position = 0; // Reset for reading lines
            return true;
        }

        public async IAsyncEnumerable<string> ReadLinesAsync(Stream stream, CancellationToken ct)
        {
            using var reader = new StreamReader(stream);
            await reader.ReadLineAsync(); // Skip header

            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (ct.IsCancellationRequested)
                    yield break;

                yield return line;
            }
        }

        public static string[] SplitCsvLine(string line)
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

            values.Add(sb.ToString());
            return values.ToArray();
        }

        public static string GetCell(string[] cells, Dictionary<string, int> headerIndex, string col)
        {
            return headerIndex.TryGetValue(col, out int index) && index < cells.Length
                ? cells[index]
                : string.Empty;
        }
    }

    public class OfgemDataModelPostImport
    {
        public List<string> HeatNetworkId { get; set; } = new();
        public string OrganisationId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserEmailId { get; set; } = string.Empty;
        public bool IsUserExist { get; set; }
        public bool IsOrganisationExist { get; set; }
    }
}