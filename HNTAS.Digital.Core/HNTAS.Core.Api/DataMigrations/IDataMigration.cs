namespace HNTAS.Core.Api.DataMigrations
{
    public interface IDataMigration
    {
        Task RunAsync();
    }
}
