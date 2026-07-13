using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Helpers;
using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Models
{
    [ExcludeFromCodeCoverage]
    public class InvitedUserResponse
    {
        public string Id { get; set; }
        public string InviterUserId { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string? FullName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName))
                    return null;

                var formattedFirst = StringFormatter.ToTitleCaseSingleWord(FirstName ?? "");
                var formattedLast = StringFormatter.ToTitleCaseSingleWord(LastName ?? "");

                return $"{formattedFirst} {formattedLast}".Trim();
            }
        }
        public string? InvitedHnId { get; set; }
        public string? InvitedOrgId { get; set; }
        public List<ContributorRole> Roles { get; set; }
        public InvitationStatus Status { get; set; }
        public DateTime InvitedAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public DateTime? RejectedAt { get; set; }
    }
}
