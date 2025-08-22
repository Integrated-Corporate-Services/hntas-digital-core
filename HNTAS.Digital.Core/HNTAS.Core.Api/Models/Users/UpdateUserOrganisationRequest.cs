using HNTAS.Core.Api.Enums;
using System.ComponentModel.DataAnnotations;

namespace HNTAS.Core.Api.Models.Users
{
    public class UpdateUserOrganisationRequest
    {
        [Required(ErrorMessage = "First Name is required.")]
        public string FirstName { get; set; } = null!;

        [Required(ErrorMessage = "Last Name is required.")]
        public string LastName { get; set; } = null!;

        [Required(ErrorMessage = "Preferred Contact Type is required.")]
        public PreferredContactType PreferredContactType { get; set; }

        public string? LandlineNumber { get; set; }

        public string? ContactNumberExtension { get; set; }

        public string? MobileNumber { get; set; }

        [Required(ErrorMessage = "Job Title is required.")]
        public string JobTitle { get; set; } = null!;

        [Required(ErrorMessage = "At least one role is required.")]
        public UserRole Role { get; set; }

        [Required(ErrorMessage = "Organisation details are required.")]
        public OrganisationRequest Organisation { get; set; } = new OrganisationRequest();
    }
}
