namespace PreOddsApi.ExternalApis.SportMonks.Sync
{
    public sealed record SportMonksSyncJobDefinition(
        string JobKey,
        string EntityName,
        string Description,
        string? Schedule = null,
        string Provider = "sportmonks")
    {
        public static SportMonksSyncJobDefinition Create(
            string jobKey,
            string entityName,
            string description,
            string? schedule = null)
        {
            return new SportMonksSyncJobDefinition(jobKey, entityName, description, schedule);
        }
    }
}
