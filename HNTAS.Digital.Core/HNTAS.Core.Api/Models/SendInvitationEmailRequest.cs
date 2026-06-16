using System.ComponentModel.DataAnnotations;

namespace HNTAS.Core.Api.Models
{
    public class SendInvitationEmailRequest
    {
        [Required(ErrorMessage = "Token is required.")]
        public string Token { get; set; } = null!;
    }
}
