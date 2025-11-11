namespace HNTAS.Core.Api.Configuration
{
    public class NotificationSettings
    {        
        public string OrgCreatedEmailTemplateId { get; set; } = null!;
        public string OrgDetailsUpdatedEmailTemplateId { get; set; } = null!;
        public string ContributorInvitationTemplatedId { get; set; } = null!;
        public string AssessorNotificationTemplatedId { get; set; } = null!;
        public string AssessmentCompleteNotificationTemplatedId { get; set; } = null!;
        public string CertificationCompleteNotificationTemplatedId { get; set; } = null!;
    }
}
