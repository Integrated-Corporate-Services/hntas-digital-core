namespace HNTAS.Core.Api.Models
{
    public sealed record AcceptInvitationResult(string UserId, bool IsCreated, bool IsNotFound)
    {
        public static AcceptInvitationResult Created(string id) => new(id, true, false);
        public static AcceptInvitationResult Updated(string id) => new(id, false, false);
        public static AcceptInvitationResult NotFound() => new(null!, false, true);
    }

}
