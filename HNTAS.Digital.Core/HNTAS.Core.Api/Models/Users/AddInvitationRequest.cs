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

        [Required(ErrorMessage = "HnId is required.")]
        public string HnId { get; set; } = null!;

        [Required(ErrorMessage = "Select at least one role.")]
        [MinLength(1, ErrorMessage = "Select at least one role.")]
        public List<ContributorRole> ContributorRoles { get; set; } = new();

        public string? CurrentRoleUserId { get; set; }

        [Required(ErrorMessage = "Status is required.")]
        public InvitationStatus Status { get; set; }
    }
}
