namespace HNTAS.Core.Api.Models.HeatNetwork
{
    public class ExistingNetworkRequest
    {
        public string? UserId { get; set; }
        public string? SortBy { get; set; }
        public string? SortDirection { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
