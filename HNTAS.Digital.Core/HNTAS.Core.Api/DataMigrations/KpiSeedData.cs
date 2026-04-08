
using HNTAS.Core.Api.Data.Models.Arms.Configuration;
using HNTAS.Core.Api.Enums;
using MongoDB.Driver;

namespace HNTAS.Core.Api.DataMigrations
{
    public class KpiSeedData : IDataMigration
    {
        private readonly IMongoCollection<KpiConfiguration> _configCollection;
        private readonly ILogger<KpiSeedData> _logger;

        public KpiSeedData(IMongoDatabase database, ILogger<KpiSeedData> logger)
        {
            // Get the specific collection
            _configCollection = database.GetCollection<KpiConfiguration>("KPI_Configurations");
            _logger = logger;
        }

        public async Task RunAsync()
        {
            // 1. Check if data already exists to prevent duplicates
            var count = await _configCollection.CountDocumentsAsync(_ => true);
            if (count > 0)
            {
                _logger.LogInformation("KPI Seed: Data already exists. Skipping migration.");
                return;
            }

            // 2. Define your seed data
            var seedConfigs = new List<KpiConfiguration>
            {
                new KpiConfiguration
                {
                    NetworkId = "HN2000001",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = null,
                    Elements = new List<KpiNetworkElement>
                    {
                        new KpiNetworkElement
                        {
                            Type = ElementType.EnergyCentre,
                            Kpis = new Dictionary<string, KpiRule>
                            {
                                ["EC-KPI-01"] = new KpiRule
                                {
                                    Unit = "percent",
                                    IsMandatory = true,
                                    ThresholdRule = new KpiThresholdRule { Type = "gte", Value = 98.5 },
                                    UpperLimit = 100,
                                    LowerLimit = 0
                                }
                            }
                        },
                        new KpiNetworkElement
                        {
                            Type = ElementType.ConsumerConnection,
                            Kpis = new Dictionary<string, KpiRule>
                            {
                                ["CC-KPI-01"] = new KpiRule
                                {
                                    Unit = "percent",
                                    IsMandatory = true,
                                    ThresholdRule = new KpiThresholdRule { Type = "lte", Value = 96.5 },
                                    UpperLimit = 100,
                                    LowerLimit = 0
                                }
                            }
                        },
                        new KpiNetworkElement
                        {
                            Type = ElementType.DistrictDistribution,
                            Kpis = new Dictionary<string, KpiRule>
                            {
                                ["DD-KPI-01"] = new KpiRule
                                {
                                    Unit = "percent",
                                    IsMandatory = true,
                                    ThresholdRule = new KpiThresholdRule { Type = "plus_minus", Value = 95.5 },
                                    UpperLimit = 100,
                                    LowerLimit = 0
                                }
                            }
                        },
                        new KpiNetworkElement
                        {
                            Type = ElementType.Substation,
                            Kpis = new Dictionary<string, KpiRule>
                            {
                                ["SS-KPI-01"] = new KpiRule
                                {
                                    Unit = "percent",
                                    IsMandatory = true,
                                    ThresholdRule = new KpiThresholdRule { Type = "gte", Value = 94.5 },
                                    UpperLimit = 100,
                                    LowerLimit = 0
                                }
                            }
                        },
                         new KpiNetworkElement
                        {
                            Type = ElementType.CommunalDistribution,
                            Kpis = new Dictionary<string, KpiRule>
                            {
                                ["CD-KPI-01"] = new KpiRule
                                {
                                    Unit = "percent",
                                    IsMandatory = true,
                                    ThresholdRule = new KpiThresholdRule { Type = "gte", Value = 91.5 },
                                    UpperLimit = 100,
                                    LowerLimit = 0
                                }
                            }
                        },
                    }
                }
            };

            // 3. Insert into MongoDB
            try
            {
                await _configCollection.InsertManyAsync(seedConfigs);
                _logger.LogInformation("KPI Seed: Successfully inserted {Count} configurations.", seedConfigs.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "KPI Seed: Failed to insert seed data.");
                throw;
            }
        }
    }
}
