using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

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

        private async Task<string> GetUuid(string hnId, HttpClient client, CancellationToken ct)
        {
            _logger.LogInformation("Entered GetUuid");
            var getUuidRequestBody = new { token = API_TOKEN, network_id = hnId };
            using var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(getUuidRequestBody),
                                                 Encoding.UTF8, "application/json");

            var response = await client.PostAsync(UUID_ENDPOINT, content, ct);
            var contentStr = await response.Content.ReadAsStringAsync(ct);
            _logger.LogInformation("Response received(GetUuid): {contentStr}", contentStr);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(contentStr);
                return "";
            }
            _logger.LogInformation("Leaving GetUuid: {contentStr}", contentStr);
            return contentStr;
        }

        // Fix for CS1503: Use JsonSerializer to serialize the request object to JSON string, then parse it as JsonNode
        public async Task<CarbonCalculatorResponse?> RunAsync(CarbonCalculatorRequest request, CancellationToken ct = default)
        {
            _logger.LogInformation("Entering RunAsync");
            var hnId = request.Background.NetworkID;
            if (string.IsNullOrWhiteSpace(API_TOKEN))
            {
                _logger.LogError("API token not configured. Set {EnvVar}.", API_TOKEN);
                return null;
            }
            _logger.LogInformation("API_TOKEN read: {API_TOKEN}", API_TOKEN);
            var govukkey = Environment.GetEnvironmentVariable("GOV_NOTIFY_API_KEY");
            var osapikey = Environment.GetEnvironmentVariable("OS_API_KEY");
            _logger.LogInformation("Gov uk API key: {govukkey}", govukkey);
            _logger.LogInformation("Gov uk API key: {osapikey}", osapikey);
            var client = _httpClientFactory.CreateClient(nameof(CarbonCalculatorService));
            if (client.BaseAddress is null)
                client.BaseAddress = new Uri(DEFAULT_BASE_URL);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            string uuid = await GetUuid(hnId, client, ct);
            _logger.LogInformation("Uuid read: {uuid}", uuid);
            try
            {
                // Serialize the request object to JSON string, then parse it as JsonNode
                string requestJson = JsonSerializer.Serialize(request);
                JsonNode bodyNode = JsonNode.Parse(requestJson) ?? new JsonObject();
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
                _logger.LogInformation("Calling Carbondescent with req: {req}", req);
                var resp = await client.SendAsync(req, ct);
                var body = await resp.Content.ReadAsStringAsync(ct);

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogError("Calculation failed. Status={Status}, Body={Body}", (int)resp.StatusCode, body);
                    return null;
                }

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                var formatted = JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
                _logger.LogInformation("Calculation response: (parsed body){formatted}", formatted);
                JsonElement calculation;

                if (!root.TryGetProperty("calculation", out calculation))
                {
                    var formattedRoot = JsonSerializer.Serialize(root, new JsonSerializerOptions { WriteIndented = true });
                    _logger.LogError("Missing 'calculation' in response : {formattedRoot}", formattedRoot );
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
                    _logger.LogInformation("All good. INTENSITY: {intensity}", intensity);
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

        public async Task CreateAsync(HnCarbonCalculation newCalculation, CancellationToken ct = default) =>
            await _hnCarbonCalculationsCollection.InsertOneAsync(newCalculation, cancellationToken: ct);
    }
}