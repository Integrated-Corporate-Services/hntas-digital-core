namespace HNTAS.Core.Api.Enums
{
    public enum NotificationHistoryType
    {
        RpInvitesNetworkManager = 1,
        NetworkManagerAcceptsInvite,
        NetworkManagerRejectsInvite,
        RpRegistersHeatNetwork,
        NetworkManagerRegistersHeatNetwork,
        RpInvitesDdhToHeatNetwork,
        RpInvitesContributorToHeatNetwork,
        NetworkManagerInvitesDdhToHeatNetwork,
        NetworkManagerInvitesContributorToHeatNetwork,
        DdhInvitesContributorToHeatNetwork,
        DdhAcceptsInviteToHeatNetwork,
        DdhRejectsInviteToHeatNetwork,
        ContributorAcceptsInviteToHeatNetwork,
        ContributorRejectsInviteToHeatNetwork,
        AssessorAssignsToHeatNetwork,
        NA
    }
}
