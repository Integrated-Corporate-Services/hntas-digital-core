using HNTAS.Core.Api.Enums;
using System.ComponentModel.DataAnnotations;

namespace HNTAS.Core.Api.Models.Users
{
    public class UpdateUserOrganisationRequest : UpdateUserDetailsRequest
    {
        [Required(ErrorMessage = "Organisation details are required.")]
        public OrganisationRequest Organisation { get; set; } = new OrganisationRequest();

        [Required(ErrorMessage = "User role is required.")]
        public new UserRole Role { get; set; }
    }
}
