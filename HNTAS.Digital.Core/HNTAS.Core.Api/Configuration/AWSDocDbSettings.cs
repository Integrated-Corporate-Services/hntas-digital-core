namespace HNTAS.Core.Api.Configuration
{
    public class AWSDocDbSettings
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
        public string UsersCollectionName { get; set; }
        public string OrganisationsCollectionName { get; set; }
        public string InvitationsCollectionName { get; set; }
        public string CountersCollectionName { get; set; }
        public string HeatNetworksCollectionName { get; set; }

        public string SoaProjectCollectionName { get; set; }
    }
}
