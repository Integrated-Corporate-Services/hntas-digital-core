using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Models;
using HNTAS.Core.Api.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace HNTAS.Core.Api.Services
{
    public interface ICarbonCalculatorService
    {
        Task<CarbonCalculatorResponse?> RunAsync(CarbonCalculatorRequest hnId, CancellationToken ct = default);
    }

    public sealed class CarbonCalculatorService : ICarbonCalculatorService
    {
        // Configs inside the class
        private const string DEFAULT_BASE_URL = "https://heatnetworkcarbon.carbondescent.org.uk";
        private const string UUID_ENDPOINT = "/api/getCalculationUUID";         // e.g. "/calculation/uuid" or similar
        private const string CALC_ENDPOINT = "/api/calculateNetwork";  // e.g. "/calculation/run" or similar

        private const string DEFAULT_REQUEST_JSON = 
            """
            {
                "background": {
                    "dateWorkbookCompleted": "2025-11-10",
                    "networkStatus": "existing",
                    "networkServiceProvision": "both",
                    "name": "Sample API Call",
                    "networkID": "SampleAPI0001",
                    "networkName": "Sample API Network",
                    "networkNamePrevious": null,
                    "heatNetworkZone": null,
                    "addressOfThePrimaryEnergyCentre": null,
                    "postcodeOfThePrimaryEnergyCentre": "AA00 0A1",
                    "networkOperator": "Carbon Descent",
                    "networkOperatorContact": "Kevin Woolley",
                    "contactTelephone": "07595 111111",
                    "contactEmail": "admin@sample.com",
                    "descriptionOfNetwork": "A sample network to show API call usage",
                    "dateOfInitialOperation": "2025-09-11",
                    "commissioningDate": "2025-09-14"
                    },
                "energy": {
                    "yearCount": 1,
                    "startYear": 2024,
                    "energyHeatNetworkPrimaryLosses": [10],
                    "chpCount": 1,
                    "chpInputs": [ 
                        {
                            "chpFuelTypeInput": "17",
                            "chpInstallationDateInput": "2025-09-16",
                            "chpOperationalModeInput": "export",
                            "chpUsefulHeatValue": ["100"],
                            "chpUsefulHeatNotes": "Useful Heat CHP",
                            "chpElectricityGeneratedValue": ["100"],
                            "chpElectricityGeneratedNotes": "Electricity CHP",
                            "chpFuelUsedValue": ["1000"],
                            "chpFuelUsedNotes": "Fuel CHP",
                            "chpHeatCoolingValue": ["100"],
                            "chpHeatCoolingNotes": "Cooling CHP",
                            "chpSleevingPCentValue": ["0"],
                            "chpSleevingPCentNotes": "Sleeving CHP",
                            "chpMaxHeatOutput": "1000",
                            "chpMaxElectricityOutput": "1200"
                        }
                    ],
                    "heatPumpCount": 1,
                    "heatPumpInputs": [ 
                        {
                            "hpmTypeFuelUsedInput": "11",
                            "hpmUsefulHeatGeneratedValue": ["1000"],
                            "hpmUsefulHeatGeneratedNotes": "Useful Heat HP",
                            "hpmEnergyUsedValue": ["1000"],
                            "hpmEnergyUsedNotes": "Energy HP",
                            "hpmUsefulCoolingGeneratedValue": ["1000"],
                            "hpmUsefulCoolingGeneratedNotes": "Cooling HP",
                            "hpmSleevingPCentValue": ["0"],
                            "hpmSleevingPCentNotes": "Sleeving HP",
                            "hpmMaxHeatOutput": "1000"
                        }
                    ],
                    "recoveredCount": 1,
                    "recoveredInputs": [ 
                        {
                            "hrwHeatRecoverySourceInput": "1",
                            "hrwUsefulHeatGeneratedValue": ["1000"],
                            "hrwUsefulHeatGeneratedNotes": "Useful Heat HR",
                            "hrwHeatUsedByCoolingProductionValue": ["1000"],
                            "hrwHeatUsedByCoolingProductionNotes": "Cooling HR",
                            "hrwSleevingPCentValue": ["0"],
                            "hrwSleevingPCentNotes": "Sleeving HR",
                            "hrwMaxHeatOutput": "1000"
                        }
                    ],
                    "boilerCount": 1,
                    "boilerInputs": [ 
                        {
                            "blrTypeFuelUsedInput": "17",
                            "blrUsefulHeatGeneratedValue": ["1000"],
                            "blrUsefulHeatGeneratedNotes": "Useful Heat BLR",
                            "blrFuelUsedByValue": ["1000"],
                            "blrFuelUsedByNotes": "Fuel BLR",
                            "blrHeatUsedForCoolingProductionValue": ["1000"],
                            "blrHeatUsedForCoolingProductionNotes": "Cooling BLR",
                            "blrSleevingPCentValue": ["0"],
                            "blrSleevingPCentNotes": "Sleeving BLR",
                            "blrMaxHeatOutput": "1000"
                        }
                    ],
                    "eppElectricityUsedForPumpingValue": [157],
                    "eppElectricityUsedForPumpingNotes": "Electricity Pump",
                    "eppSleevingPCentNotes": "Sleeving Pump"
                }
            }            
            """;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<CarbonCalculatorService> _logger;
        private readonly string API_TOKEN;

        public readonly IMongoCollection<HnCarbonCalculation> _hnCarbonCalculationsCollection;

        public CarbonCalculatorService(IHttpClientFactory httpClientFactory, IOptions<AWSDocDbSettings> dbSettings, ILogger<CarbonCalculatorService> logger)
        {
            API_TOKEN = Environment.GetEnvironmentVariable("HEATNETWORK_CARBON_API_TOKEN") ?? throw new ArgumentNullException("HEATNETWORK_CARBON_API_TOKEN environment variable is not set.");
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            var connectionString = Environment.GetEnvironmentVariable("DOCUMENT_DB_CONNECTION_STRING");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("MongoDB connection string is not configured. Set 'DOCUMENT_DB_CONNECTION_STRING' environment variable.");
            }

            _logger.LogInformation("Initializing UserService with connection string: {ConnectionString}", connectionString);

            var mongoClient = new MongoClient(connectionString);
            var mongoDatabase = mongoClient.GetDatabase(dbSettings.Value.DatabaseName);

            _hnCarbonCalculationsCollection = mongoDatabase.GetCollection<HnCarbonCalculation>(dbSettings.Value.HnCarbonCalculationsCollectionName);
        }

        private async Task<string> GetUuid(string hnId, HttpClient client)
        {
            // Prepare request body
            var getUuidRequestBody = new
            {
                token = API_TOKEN,
                network_id = hnId
            };

            // Send POST request
            var response = await client.PostAsync(UUID_ENDPOINT,
                new StringContent(System.Text.Json.JsonSerializer.Serialize(getUuidRequestBody),
                Encoding.UTF8, "application/json"));

            // Check status
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(content);
                return "";
            }
            else
            {
                return content;
            }
        }

        public async Task<CarbonCalculatorResponse?> RunAsync(CarbonCalculatorRequest request, CancellationToken ct = default)
        {
            var hnId = request.Background.NetworkID;
            if (string.IsNullOrWhiteSpace(API_TOKEN))
            {
                _logger.LogError("API token not configured. Set {EnvVar}.", API_TOKEN);
                return null;
            }

            var client = _httpClientFactory.CreateClient(nameof(CarbonCalculatorService));
            if (client.BaseAddress is null)
                client.BaseAddress = new Uri(DEFAULT_BASE_URL);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            string uuid = await GetUuid(hnId, client);

            try
            {
                JsonNode bodyNode = JsonNode.Parse(DEFAULT_REQUEST_JSON) ?? new JsonObject();
                if (bodyNode is not JsonObject obj) obj = new JsonObject();

                var bg = obj["background"] as JsonObject ?? new JsonObject();
                bg["uuid"] = uuid;
                bg["networkID"] = hnId;
                obj["background"] = bg;
                obj["token"] = API_TOKEN;

                var json = obj.ToJsonString(new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                using var req = new HttpRequestMessage(HttpMethod.Post, CALC_ENDPOINT)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                var resp = await client.SendAsync(req, ct);
                var body = await resp.Content.ReadAsStringAsync(ct);

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogError("Calculation failed. Status={Status}, Body={Body}", (int)resp.StatusCode, body);
                    return null;
                }

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                JsonElement calculation;
                if (root.TryGetProperty("result", out var result) &&
                    result.TryGetProperty("calculation", out calculation))
                {
                    // Found under result.calculation
                }
                else if (!root.TryGetProperty("calculation", out calculation))
                {
                    _logger.LogError("Missing 'calculation' in response.");
                    return null;
                }

                if (!calculation.TryGetProperty("energy", out var energy) ||
                    !energy.TryGetProperty("energyOverallHeatNetworkIntensity", out var arr) ||
                    arr.ValueKind != JsonValueKind.Array || arr.GetArrayLength() == 0)
                {
                    _logger.LogError("Missing intensity array.");
                    return null;
                }

                var str = arr[0].GetString();
                if (decimal.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var intensity))
                {
                    var hnCarbonCalculation = new HnCarbonCalculation
                    {
                        HnId = hnId,
                        Uuid = uuid,
                        TotalCarbonEmission = intensity,
                        CreatedUtc = DateTime.UtcNow
                    };
                    await CreateAsync(hnCarbonCalculation);
                    return new CarbonCalculatorResponse
                    {
                        HnId = hnId,
                        Uuid = uuid,
                        TotalCarbonEmission = intensity
                    };
                }
                    

                _logger.LogError("Failed to parse intensity value: {Value}", str);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing calculation.");
                return null;
            }
        }

        public async Task CreateAsync(HnCarbonCalculation newCalculation) =>
            await _hnCarbonCalculationsCollection.InsertOneAsync(newCalculation);
    }
}