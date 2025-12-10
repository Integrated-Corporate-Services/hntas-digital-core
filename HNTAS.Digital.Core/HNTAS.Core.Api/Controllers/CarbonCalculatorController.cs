using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HNTAS.Core.Api.Controllers
{
    [ApiController]
    [Route("api/carbon-calc")]
    public class CarbonCalculatorController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<CarbonCalculatorController> _logger;

        // TODO: Replace these with the exact values from the API guide
        private const string DEFAULT_BASE_URL = "https://heatnetworkcarbon.carbondescent.org.uk";
        private const string UUID_ENDPOINT = "/api/getCalculationUUID";         // e.g. "/calculation/uuid" or similar
        private const string CALC_ENDPOINT = "/api/calculateNetwork";  // e.g. "/calculation/run" or similar

        // Paste the "default example request body" from the API guide here (as valid JSON)
        // Then we will override heatnetwork_id and add uuid dynamically.

        private const string DEFAULT_REQUEST_JSON = """
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
                
        public CarbonCalculatorController(IHttpClientFactory httpClientFactory,
                                          ILogger<CarbonCalculatorController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        private async Task<string> GetUuid(string token, string hnId, HttpClient client)
        {
            // Prepare request body
            var getUuidRequestBody = new
            {
                token,
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

        /// <summary>
        /// Calls the UUID generator, then runs the carbon calculation with the generated UUID.
        /// - Token is taken from env var HEATNETWORK_CARBON_API_TOKEN
        /// - Request body is the API guide's default example (overriding heatnetwork_id if provided)
        /// </summary>
        [HttpPost("run")]
        public async Task<IActionResult> RunAsync([FromQuery] string hnId = "HN1234567")
        {
            // 1) Read API token from environment variable
            var token = Environment.GetEnvironmentVariable("HEATNETWORK_CARBON_API_TOKEN");
            if (string.IsNullOrWhiteSpace(token))
            {
                return Problem("API token not configured. Set HEATNETWORK_CARBON_API_TOKEN environment variable.");
            }
            // Create HttpClient
            using var client = new HttpClient { BaseAddress = new Uri(DEFAULT_BASE_URL) };
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            string uuid = await GetUuid(token, hnId, client);

            // 4) Prepare the calculation request body
            JsonNode bodyNode;
            try
            {
                bodyNode = JsonNode.Parse(DEFAULT_REQUEST_JSON) ?? new JsonObject();
            }
            catch (Exception parseEx)
            {
                _logger.LogError(parseEx, "DEFAULT_REQUEST_JSON is not valid JSON.");
                return Problem("DEFAULT_REQUEST_JSON is not valid JSON. Please paste the guide's default example JSON.");
            }

            // Ensure root is an object
            if (bodyNode is not JsonObject obj)
                bodyNode = obj = new JsonObject();

            // Override heatnetwork_id with user-provided (or default) hnId
            obj["heatnetwork_id"] = hnId;

            // If the API expects the UUID in the body, include it here
            // (If the API expects it in path/query/header instead, adjust accordingly)
            var bg = obj["background"];
            bg["uuid"] = uuid;
            obj["background"] = bg;
            obj["token"] = token; // Ensure token is included if required]

            var json = bodyNode.ToJsonString(new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            // 5) Call the calculation endpoint
            try
            {
                using var calcReq = new HttpRequestMessage(HttpMethod.Post, CALC_ENDPOINT)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                var calcResp = await client.SendAsync(calcReq);
                var calcBody = await calcResp.Content.ReadAsStringAsync();

                if (!calcResp.IsSuccessStatusCode)
                {
                    _logger.LogError("Calculation request failed. Status: {Status} Body: {Body}",
                        (int)calcResp.StatusCode, calcBody);
                    return Problem(
                        title: "Calculation failed",
                        detail: calcBody,
                        statusCode: (int)calcResp.StatusCode);
                }

                // Try to return parsed JSON; if not JSON, return as text
                try
                {

                    using var doc = JsonDocument.Parse(calcBody);

                    var value = doc.RootElement
                        .GetProperty("calculation")
                        .GetProperty("energy")
                        .GetProperty("energyOverallHeatNetworkIntensity")[0]
                        .GetString();

                    //var paredRootElement = JsonDocument.Parse(parsedCalcBody.RootElement);
                    return Ok(new
                    {
                        energyOverallHeatNetworkIntensity = value
                    });
                }
                catch
                {
                    // Not JSON, return as plain text payload
                    return Ok(new { uuid, result = calcBody });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error when calling calculation endpoint.");
                return Problem("Error executing calculation: " + ex.Message);
            }
        }
    }
}
