using HNTAS.Core.Api.Enums;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Models.Users
{
    [ExcludeFromCodeCoverage]
    public class UpdateUserDetailsRequest
    {
        [Required(ErrorMessage = "First Name is required.")]
        public string FirstName { get; set; } = null!;

        [Required(ErrorMessage = "Last Name is required.")]
        public string LastName { get; set; } = null!;

        public PreferredContactType? PreferredContactType { get; set; }

        public string? LandlineNumber { get; set; }

        public string? ContactNumberExtension { get; set; }

        public string? MobileNumber { get; set; }

        [Required(ErrorMessage = "Job Title is required.")]
        public string JobTitle { get; set; } = null!;

        public UserRole? Role { get; set; }
    }
}
