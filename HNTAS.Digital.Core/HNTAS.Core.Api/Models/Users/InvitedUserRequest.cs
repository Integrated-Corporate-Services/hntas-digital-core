using System.ComponentModel.DataAnnotations;

namespace HNTAS.Core.Api.Models.Users
{
    public class InvitedUserRequest
    {
        [Required(ErrorMessage = "InvitedEmail is required.")]
        public string InvitedEmail { get; set; } = null!;

        [Required(ErrorMessage = "InvitationId is required.")]
        public string InvitationId { get; set; } = null!;

        [Required(ErrorMessage = "OneLoginId is required.")]
        public string OneLoginId { get; set; } = null!;

        [Required(ErrorMessage = "OrgId is required.")]
        public string InviterOrgId { get; set; } = null!;
    }
}
