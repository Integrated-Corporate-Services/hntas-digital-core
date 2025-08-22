using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using System.ComponentModel.DataAnnotations;

namespace HNTAS.Core.Api.Models.Users
{
    public class OrganisationRequest
    {
        [Required(ErrorMessage = "Organisation Name is required.")]
        public string Name { get; set; } = null!;

        public string? CompaniesHouseNumber { get; set; }

        [Required(ErrorMessage = "Organisation Type is required.")]
        public OrganisationType Type { get; set; }

        [Required(ErrorMessage = "Organisation Registered Address is required.")]
        public RegisteredAddress RegisteredAddress { get; set; } = null!;
    }
}
