using AutoMapper;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Extensions;
using HNTAS.Core.Api.Helpers;
using HNTAS.Core.Api.Models;
using HNTAS.Core.Api.Models.Soa;
using HNTAS.Core.Api.Models.Users;

namespace HNTAS.Core.Api.MappingProfiles
{
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {

            // Mapping for a User document to a UserResponse DTO
            // This mapping no longer relies on nested data.
            CreateMap<User, UserResponse>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.EmailId, opt => opt.MapFrom(src => src.EmailId))
                .ForMember(dest => dest.OneLoginId, opt => opt.MapFrom(src => src.OneLoginId))
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src =>
                    (src.FirstName != null && src.LastName != null)
                    ? $"{StringFormatter.ToTitleCaseSingleWord(src.FirstName)} {StringFormatter.ToTitleCaseSingleWord(src.LastName)}"
                    : null
                ))
                .ForMember(dest => dest.JobTitle, opt => opt.MapFrom(src => src.JobTitle))
                .ForMember(dest => dest.PreferredContactType, opt => opt.MapFrom(src =>
                    src.PreferredContactType != null ? src.PreferredContactType : null
                ))
                .ForMember(dest => dest.LandlineNumber, opt => opt.MapFrom(src => src.LandlineNumber))
                .ForMember(dest => dest.MobileNumber, opt => opt.MapFrom(src => src.MobileNumber))
                .ForMember(dest => dest.ContactNumberExtension, opt => opt.MapFrom(src => src.ContactNumberExtension))
                .ForMember(dest => dest.OrgId, opt => opt.MapFrom(src => src.OrgId))
                .ForMember(dest => dest.HnIds, opt => opt.MapFrom(src => src.HnIds))
                .ForMember(dest => dest.Roles, opt => opt.MapFrom(src =>
                    src.Roles != null ? src.Roles.Select(role => role).ToList() : null
                ))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src =>
                    src.Status
                ));


            // Mapping for an Invitation document to an InvitedUserResponse DTO
            CreateMap<Invitation, InvitedUserResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.InvitedEmail))
            .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => src.InvitedRoles.ToList()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.InvitedAt, opt => opt.MapFrom(src => src.InvitedAt))
            .ForMember(dest => dest.AcceptedAt, opt => opt.MapFrom(src => src.AcceptedAt))
            .ForMember(dest => dest.RejectedAt, opt => opt.MapFrom(src => src.RejectedAt));


            CreateMap<HeatNetwork, HeatNetworkResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.HnId, opt => opt.MapFrom(src => src.HnId))
            .ForMember(dest => dest.Location, opt => opt.MapFrom(src => src.Location))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Pathway, opt => opt.MapFrom(src => src.Pathway))
            .ForMember(dest => dest.Soa, opt => opt.MapFrom(src => src.Soa));

            CreateMap<Soa, SoaResponse>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt))
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy))
            .ForMember(dest => dest.JourneyData, opt => opt.MapFrom(src => src.JourneyData));

            CreateMap<SoaJourneyData, JourneyDataResponse>()
            .ForMember(dest => dest.NetworkType, opt => opt.MapFrom(src => src.NetworkType))
            .ForMember(dest => dest.ConnectionTypes, opt => opt.MapFrom(src => src.ConnectionTypes))
            .ForMember(dest => dest.HeatNetworkElements, opt => opt.MapFrom(src => src.HeatNetworkElements))
            .ForMember(dest => dest.AssessmentDocs, opt => opt.MapFrom(src => src.AssessmentDocs))
            .ForMember(dest => dest.AssessorDocs, opt => opt.MapFrom(src => src.AssessorDocs))
            .ForMember(dest => dest.CertifierDocs, opt => opt.MapFrom(src => src.CertifierDocs));

            CreateMap<NetworkTypeSelection, NetworkTypeResponse>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()))
            .ForMember(dest => dest.OtherNetworkDescription, opt => opt.MapFrom(src => src.OtherNetworkDescription));

            CreateMap<HeatNetworkElement, HeatNetworkElementResponse>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Count, opt => opt.MapFrom(src => src.Count))
            .ForMember(dest => dest.Locations, opt => opt.MapFrom(src => src.Locations))
            .ForMember(dest => dest.Documents, opt => opt.MapFrom(src => src.Documents));

            CreateMap<UploadedDocument, UploadedDocumentResponse>()
            .ForMember(dest => dest.FileName, opt => opt.MapFrom(src => src.FileName))
            .ForMember(dest => dest.S3Key, opt => opt.MapFrom(src => src.S3Key))
            .ForMember(dest => dest.Phase, opt => opt.MapFrom(src => src.Phase))
            .ForMember(dest => dest.Stage, opt => opt.MapFrom(src => src.Stage))
            .ForMember(dest => dest.UploadedAt, opt => opt.MapFrom(src => src.UploadedAt))
            .ForMember(dest => dest.UploadedBy, opt => opt.MapFrom(src => src.UploadedBy));


            CreateMap<UploadedDocument, UploadedAssessmentDocumentResponse>();
            CreateMap<UploadedDocument, UploadedAssessorDocumentResponse>();
            CreateMap<UploadedDocument, UploadedCertifierDocumentResponse>();



            CreateMap<UserDetailsResponse, ManagedUserResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.FullName ?? $"{src.FirstName} {src.LastName}".Trim()))
            .ForMember(dest => dest.EmailId, opt => opt.MapFrom(src => src.EmailId))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => src.Roles != null ? src.Roles.Select(r => r.ToString()).ToList() : null))
            .ForMember(dest => dest.HeatNetworks, opt => opt.MapFrom(src => src.HeatNetworks));

            // Map from HeatNetworkUserResponse to HeatNetworkInfo
            CreateMap<HeatNetworkUserResponse, HeatNetworkInfo>()
                .ForMember(dest => dest.HnId, opt => opt.MapFrom(src => src.HnId))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name));

            CreateMap<User, UserRoleDetailResponse>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}".Trim()))
                .ForMember(dest => dest.RoleDescription, opt => opt.MapFrom(src => UserRole.ResponsiblePerson.GetDescription()));
        }

    }
}
