using System;
using System.Threading;
using System.Threading.Tasks;
using PreOddsApi.WebApi.V3.Dtos;

namespace PreOddsApi.WebApi.V3.Data
{
    /// <summary>
    /// Bridges a verified social-provider identity (Apple / Google) into
    /// an app.users row. Identity dedup happens on
    /// (provider, provider_subject) — Apple's `sub` claim is stable, so
    /// linking the same Apple ID twice always lands on the same user.
    /// </summary>
    public interface ISocialIdentityService
    {
        /// <summary>
        /// Looks up an existing user by (provider, providerSubject) or
        /// creates one. Returns the (now-current) UserDto including tier.
        /// </summary>
        Task<UserDto> UpsertFromProviderAsync(
            string provider,
            string providerSubject,
            string? email,
            string? displayName,
            CancellationToken ct = default);
    }
}
