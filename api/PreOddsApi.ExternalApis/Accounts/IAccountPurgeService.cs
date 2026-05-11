using System.Threading;
using System.Threading.Tasks;

namespace PreOddsApi.ExternalApis.Accounts
{
    /// <summary>
    /// Background hard-delete pass for soft-deleted accounts. Apple and
    /// Google both require an in-app delete flow that actually removes
    /// user data — we soft-delete immediately (PII scrubbed, status flipped)
    /// and a nightly worker tick hard-removes the row 30 days later.
    /// The audit row in app.account_deletions survives so the operator
    /// can still answer "did email X exist?" without holding the email.
    /// </summary>
    public interface IAccountPurgeService
    {
        Task<int> PurgeStaleAccountsAsync(
            int olderThanDays,
            CancellationToken cancellationToken = default);
    }
}
