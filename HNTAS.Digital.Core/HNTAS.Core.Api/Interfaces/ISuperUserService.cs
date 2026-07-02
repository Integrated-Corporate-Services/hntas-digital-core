namespace HNTAS.Core.Api.Interfaces
{
    public interface ISuperUserService
    {
        Task<bool> IsSuperUserAsync(string email);
    }
}
