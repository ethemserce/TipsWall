
namespace PreOddsApi.WebApi.V3.Contracts
{
    /// <summary>
    /// Mobile-minted stable device identifier. UUID/v4 generated on first
    /// launch and persisted to expo-secure-store; the same value is sent
    /// for every quota call so a single device's daily counter survives
    /// across app re-opens.
    /// </summary>
    public sealed class GuestQuotaClaimRequest
    {
        public string DeviceId { get; set; } = string.Empty;
    }
}
