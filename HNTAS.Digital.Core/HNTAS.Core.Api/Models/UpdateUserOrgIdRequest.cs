using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Models
{
    [ExcludeFromCodeCoverage]
    public class UpdateUserOrgIdRequest
    {
        [Required]
        [StringLength(24, MinimumLength = 24, ErrorMessage = "UserId must be a valid 24-character ObjectId.")]
        public string UserId { get; set; } = null!;

        [Required]
        public string OrgId { get; set; } = null!;
    }
}
