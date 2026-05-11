
namespace PreOddsApi.WebApi.V3.Contracts
{
    /// <summary>
    /// Optional payload for the in-app "delete my account" flow. Apple
    /// and Google both require this entry point. The reason field is
    /// free-text feedback — we store it on app.account_deletions so the
    /// product team can spot patterns. Empty body is fine.
    /// </summary>
    public sealed class DeleteAccountRequest
    {
        public string? Reason { get; set; }
    }
}
