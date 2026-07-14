using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Models.Users
{
    [ExcludeFromCodeCoverage]
    public class InvitedUserRequest
    {

        [Required(ErrorMessage = "InvitationId is required.")]
        public string InvitationId { get; set; } = null!;

        [Required(ErrorMessage = "OneLoginId is required.")]
        public string OneLoginId { get; set; } = null!;
    }
}
