using HNTAS.Core.Api.Enums;
using System.ComponentModel.DataAnnotations;

namespace HNTAS.Core.Api.Models.Users
{
    public class AddInvitationRequest
    {
        [Required(ErrorMessage = "EmailAddress is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string EmailAddress { get; set; } = null!;

        [Required(ErrorMessage = "First Name is required.")]
        public string FirstName { get; set; } = null!;
        [Required(ErrorMessage = "Last Name is required.")]
        public string LastName { get; set; } = null!;

        public string? HnId { get; set; }

        public string? OrgId { get; set; }

        public List<ContributorRole>? ContributorRoles { get; set; } = new();

        public string? CurrentRoleUserId { get; set; }

        [Required(ErrorMessage = "Status is required.")]
        public InvitationStatus Status { get; set; }
    }
}
