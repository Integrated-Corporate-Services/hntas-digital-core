using HNTAS.Core.Api.Helpers;

namespace HNTAS.Core.Api.Models
{
    public class InvitedUserResponse
    {
        public string Id { get; set; }
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

        public List<string> Roles { get; set; }
        public string Status { get; set; }
        public DateTime InvitedAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public DateTime? RejectedAt { get; set; }
    }
}
