namespace HNTAS.Core.Api.Configuration
{
    public class AWSDocDbSettings
    {
        public string ConnectionString { get; set; } = null!;
        public string DatabaseName { get; set; } = null!;
        public string UsersCollectionName { get; set; } = null!;
        public string OrganisationsCollectionName { get; set; } = null!;
        public string InvitationsCollectionName { get; set; } = null!;
        public string CountersCollectionName { get; set; } = null!;
        public string HeatNetworksCollectionName { get; set; } = null!;
        public string CountriesAndTerritoriesCollectionName { get; set; } = null!;
        public string HnCarbonCalculationsCollectionName { get; set; } = null!;
        public string AssessorsCollectionName { get; set; } = null!;
        public string NotificationHistoryCollectionName { get; set; } = null!;
        public string UserStatsCollectionName { get; set; } = null!;

        public string SuperUsersCollectionName { get; set; } = null!;
    }
}
