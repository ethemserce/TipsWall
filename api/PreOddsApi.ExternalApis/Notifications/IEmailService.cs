using System.Threading;
using System.Threading.Tasks;

namespace PreOddsApi.ExternalApis.Notifications
{
    /// <summary>
    /// Tiny single-purpose contract for sending plain-text operational
    /// alerts (e.g. "nightly snapshot failed twice"). Not a full mailer —
    /// no templating, no attachments. Plug in a real provider later when
    /// the use case grows.
    /// </summary>
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string body, CancellationToken ct = default);

        /// <summary>
        /// True when SMTP credentials are configured. Callers can short-
        /// circuit before composing a message rather than catching a
        /// "config missing" exception inside the send.
        /// </summary>
        bool IsConfigured { get; }
    }
}
