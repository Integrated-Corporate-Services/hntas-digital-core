namespace HNTAS.Core.Api.Models.Soa
{
    public class SOAAssesorEmailRequest
    {
        public string HnId { get; set; } = null!;
        public string HnName { get; set; } = null!;
        public string Phase { get; set; } = null!;
        public string StageNumber { get; set; } = null!;
        public string StageName { get; set; } = null!;

        public string ContributorName { get; set; } = null!;
    }
}
