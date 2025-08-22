using AutoMapper;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Extensions;
using HNTAS.Core.Api.Helpers;
using HNTAS.Core.Api.Models;
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
                    src.PreferredContactType != null ? src.PreferredContactType.GetDescription() : null
                ))
                .ForMember(dest => dest.LandlineNumber, opt => opt.MapFrom(src => src.LandlineNumber))
                .ForMember(dest => dest.MobileNumber, opt => opt.MapFrom(src => src.MobileNumber))
                .ForMember(dest => dest.OrgId, opt => opt.MapFrom(src => src.OrgId))
                .ForMember(dest => dest.HnIds, opt => opt.MapFrom(src => src.HnIds))
                .ForMember(dest => dest.Roles, opt => opt.MapFrom(src =>
                    src.Roles != null ? src.Roles.Select(role => role.GetDescription()).ToList() : null
                ))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src =>
                    src.Status.GetDescription()
                ));


            // Mapping for an Invitation document to an InvitedUserResponse DTO
            CreateMap<Invitation, InvitedUserResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.InvitedEmail))
            .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => src.InvitedRoles.Select(r => r.GetDescription()).ToList()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.InvitedAt, opt => opt.MapFrom(src => src.InvitedAt))
            .ForMember(dest => dest.AcceptedAt, opt => opt.MapFrom(src => src.AcceptedAt))
            .ForMember(dest => dest.RejectedAt, opt => opt.MapFrom(src => src.RejectedAt));
        }
    }
}
