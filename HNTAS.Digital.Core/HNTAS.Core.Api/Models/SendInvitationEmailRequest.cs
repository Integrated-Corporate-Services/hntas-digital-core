using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Models
{
    [ExcludeFromCodeCoverage]
    public class SendInvitationEmailRequest
    {
        [Required(ErrorMessage = "Token is required.")]
        public string Token { get; set; } = null!;
    }
}
