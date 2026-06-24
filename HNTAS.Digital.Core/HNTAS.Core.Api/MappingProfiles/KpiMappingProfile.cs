using AutoMapper;
using HNTAS.Core.Api.Data.Models.Arms.Configuration;
using HNTAS.Core.Api.Data.Models.Arms.Submission;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Models.Arms;
using HNTAS.Core.Api.Models.Arms.V2;
using MongoDB.Bson;
using System.Text.Json;

namespace HNTAS.Core.Api.MappingProfiles
{
    public class KpiMappingProfile : Profile
    {
        public KpiMappingProfile()
        {
            // DB -> API (Response)
            CreateMap<KpiConfiguration, KpiConfigResponse>()
                .ForMember(dest => dest.NetworkId, opt => opt.MapFrom(src => src.NetworkId))
                .ForMember(dest => dest.Elements, opt => opt.MapFrom(src =>
                    src.Elements.ToDictionary(
                        e => e.Type.ToString(),
                        e => e.Kpis
                    )));

            // API (Request) -> DB
            CreateMap<KpiConfigRequest, KpiConfiguration>()
                .ForMember(dest => dest.Elements, opt => opt.MapFrom(src =>
                    src.Elements.Select(kvp => new KpiNetworkElement
                    {
                        // Converts string "EnergyCentre" back to ElementType.EnergyCentre
                        Type = Enum.Parse<HeatNetworkElementType>(kvp.Key, true),
                        Kpis = kvp.Value
                    }).ToList()));

            CreateMap<KpiConfigRequestV2, KpiConfiguration>()
             .ForMember(dest => dest.Elements, opt => opt.MapFrom(src =>
                 src.Elements.Select(kvp => new KpiNetworkElement
                 {
                     Type = Enum.Parse<HeatNetworkElementType>(kvp.Key, true),
                     Kpis = kvp.Value
                 }).ToList()))
              .ForMember<CarbonCalculatorConfig>(dest => dest.CarbonCalculator, opt => opt.MapFrom<CarbonCalculatorConfig>(src =>
                src.CarbonCalculator != null
                    ? new CarbonCalculatorConfig
                    {
                        Rules = src.CarbonCalculator.Rules != null
                              ? src.CarbonCalculator.Rules.ToDictionary(
                                  kvp => kvp.Key,
                                  kvp => kvp.Value
                                )
                              : new Dictionary<string, KpiRule>(),
                        Defaults = src.CarbonCalculator.Defaults != null
                              ? src.CarbonCalculator.Defaults.ToDictionary(
                                  kvp => kvp.Key,
                                  kvp => ConvertJsonElementToBsonValue(kvp.Value.Value)
                                )
                              : new Dictionary<string, BsonValue>()
                    }
                    : null));

            // 1. Map the nested Carbon Calculator Config objects
            CreateMap<CarbonCalculatorConfig, CarbonCalculatorConfigResponse>()
                .ForMember(dest => dest.Rules, opt => opt.MapFrom(src => src.Rules))
                .ForMember(dest => dest.Defaults, opt => opt.MapFrom(src =>
                    src.Defaults != null
                        ? src.Defaults.ToDictionary(
                            kvp => kvp.Key,
                            kvp => ConvertBsonValueToJsonElement(kvp.Value))
                        : new Dictionary<string, JsonElement>()));

            // 2. Map the main Configuration wrapper profile
            CreateMap<KpiConfiguration, KpiConfigResponseV2>()
                .ForMember(dest => dest.Elements, opt => opt.MapFrom(src =>
                    src.Elements.ToDictionary(
                        el => el.Type.ToString(),
                        el => el.Kpis)))
                .ForMember(dest => dest.CarbonCalculator, opt => opt.MapFrom(src => src.CarbonCalculator));


            CreateMap<CCKpiValueRequest, CCKpiValue>()
            .ForMember(dest => dest.Value, opt => opt.MapFrom(src => ConvertJsonElementToBsonValue(src.Value)));
            CreateMap<KpiSubmissionRequest, KpiSubmission>();
            CreateMap<KpiSubmissionRequestV2, KpiSubmission>()
             // Maps the incoming API property down to the MongoDB domain model field name
             .ForMember(dest => dest.CarbonCalculatorInputs, opt => opt.MapFrom(src => src.CarbonInputsV2));

            CreateMap<CCKpiValueRequest, CCKpiValue>()
                .ForMember(dest => dest.Value, opt => opt.MapFrom(src => ConvertJsonElementToBsonValue(src.Value)));

            CreateMap<NetworkElementRequest, NetworkElement>();
            CreateMap<KpiSubmissionRequestV2, NetworkElement>();
            CreateMap<KpiValueRequest, KpiValue>();
            CreateMap<KpiValueAggregatedRequest, KpiValueAggregated>();
        }

        private static BsonValue ConvertJsonElementToBsonValue(System.Text.Json.JsonElement element)
        {
            switch (element.ValueKind)
            {
                case System.Text.Json.JsonValueKind.String:
                    return new BsonString(element.GetString());

                case System.Text.Json.JsonValueKind.Number:
                    if (element.TryGetInt32(out var intVal)) return new BsonInt32(intVal);
                    if (element.TryGetInt64(out var longVal)) return new BsonInt64(longVal);
                    return new BsonDouble(element.GetDouble());

                case System.Text.Json.JsonValueKind.True:
                    return BsonBoolean.True;

                case System.Text.Json.JsonValueKind.False:
                    return BsonBoolean.False;

                case System.Text.Json.JsonValueKind.Null:
                    return BsonNull.Value;

                case System.Text.Json.JsonValueKind.Object:
                case System.Text.Json.JsonValueKind.Array:
                    // For complex sub-objects or arrays, parse the raw text straight into MongoDB formats
                    return BsonDocument.Parse(element.GetRawText());

                default:
                    return BsonNull.Value;
            }
        }

        private static System.Text.Json.JsonElement ConvertBsonValueToJsonElement(MongoDB.Bson.BsonValue bsonValue)
        {
            // Serialize the BsonValue to a standard JSON string format
            string jsonString;

            if (bsonValue.IsBsonDocument || bsonValue.IsBsonArray)
            {
                jsonString = bsonValue.ToJson();
            }
            else
            {
                // For primitive types (strings, numbers, booleans), format them safely as a value item
                jsonString = bsonValue.ToJson(new MongoDB.Bson.IO.JsonWriterSettings
                {
                    OutputMode = MongoDB.Bson.IO.JsonOutputMode.RelaxedExtendedJson
                });
            }

            // Parse the JSON string back into a system-native JsonElement structure
            using var doc = System.Text.Json.JsonDocument.Parse(jsonString);
            return doc.RootElement.Clone();
        }
    }
}
