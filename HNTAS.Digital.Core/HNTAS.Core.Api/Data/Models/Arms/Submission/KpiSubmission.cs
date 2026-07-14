using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Data.Models.Arms.Submission
{
    [ExcludeFromCodeCoverage]
    public class KpiSubmission
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonIgnoreIfDefault]
        public string? Id { get; set; }

        [BsonElement("metaData")]
        [JsonPropertyName("meta_data")]
        public required KpiMetadata MetaData { get; set; }

        [BsonElement("consumerConnectionAggregatedKpis")]
        [JsonPropertyName("consumer_connection_aggregated_kpis")]
        public Dictionary<string, KpiValueAggregated>? ConsumerConnectionAggregatedKpis { get; set; }

        [JsonPropertyName("carbon_calculator_inputs")]
        [BsonElement("carbonCalculatorInputs")]
        public Dictionary<string, Dictionary<string, CCKpiValue>>? CarbonCalculatorInputs { get; set; }

        [JsonPropertyName("carbon_calculator_response")]
        [BsonElement("carbonCalculatorResponse")]
        public CarbonCalculatorResponse? CarbonCalculatorResponse { get; set; }

        [JsonPropertyName("elements")]
        [BsonElement("elements")]
        public List<NetworkElement> Elements { get; set; } = new();

        [JsonPropertyName("created_at")]
        [BsonElement("createdAt")]
        [BsonRepresentation(BsonType.DateTime)]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        [BsonElement("updatedAt")]
        [BsonRepresentation(BsonType.DateTime)]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        [BsonIgnoreIfNull]
        public DateTime? UpdatedAt { get; set; }
    }

    public class CCKpiValue
    {
        [JsonPropertyName("value")]
        [BsonElement("value")]
        public BsonValue Value { get; set; } = BsonNull.Value;

        [JsonPropertyName("is_imputed")]
        [BsonElement("isImputed")]
        public bool IsImputed { get; set; } = false;

        [BsonElement("imputationDetails")]
        [JsonPropertyName("imputation_details")]
        public string? ImputationDetails { get; set; }
    }
}
