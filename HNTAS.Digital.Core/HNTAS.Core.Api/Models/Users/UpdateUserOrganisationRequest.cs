using HNTAS.Core.Api.Enums;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Models.Users
{
    [ExcludeFromCodeCoverage]
    public class UpdateUserOrganisationRequest : UpdateUserDetailsRequest
    {
        [Required(ErrorMessage = "Organisation details are required.")]
        public OrganisationRequest Organisation { get; set; } = new OrganisationRequest();

        [Required(ErrorMessage = "User role is required.")]
        public new UserRole Role { get; set; }
    }
}
