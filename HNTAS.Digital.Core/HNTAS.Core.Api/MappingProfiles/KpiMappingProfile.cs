using AutoMapper;
using HNTAS.Core.Api.Data.Models.Arms.Configuration;
using HNTAS.Core.Api.Data.Models.Arms.Submission;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Models.Arms;

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

            CreateMap<KpiSubmissionRequest, KpiSubmission>();
            CreateMap<NetworkElementRequest, NetworkElement>();
            CreateMap<KpiValueRequest, KpiValue>();
            CreateMap<KpiValueAggregatedRequest, KpiValueAggregated>();
        }
    }
}
