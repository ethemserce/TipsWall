using System;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace PreOddsApi.ExternalApis.Notifications
{
    /// <summary>
    /// SMTP-backed email sender via MailKit. Defaults match GoDaddy
    /// Professional Email (smtpout.secureserver.net:465 SslOnConnect)
    /// but every value is overridable from configuration. Credentials
    /// are read once at construction; rotating them needs a process
    /// restart (the worker scheduler is fine with that).
    /// </summary>
    public sealed class SmtpEmailService : IEmailService
    {
        private readonly string? _host;
        private readonly int _port;
        private readonly SecureSocketOptions _security;
        private readonly string? _username;
        private readonly string? _password;
        private readonly string _from;
        private readonly string _fromName;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
        {
            _logger = logger;
            _host = Environment.GetEnvironmentVariable("MAIL_HOST")
                ?? configuration["Mail:Host"]
                ?? "smtpout.secureserver.net";
            _port = int.TryParse(
                Environment.GetEnvironmentVariable("MAIL_PORT")
                    ?? configuration["Mail:Port"], out var p) ? p : 465;
            _security = (_port == 465)
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls;
            _username = Environment.GetEnvironmentVariable("MAIL_USERNAME")
                ?? configuration["Mail:Username"];
            _password = Environment.GetEnvironmentVariable("MAIL_PASSWORD")
                ?? configuration["Mail:Password"];
            _from = Environment.GetEnvironmentVariable("MAIL_FROM")
                ?? configuration["Mail:From"]
                ?? _username
                ?? "noreply@tipswall.com";
            _fromName = Environment.GetEnvironmentVariable("MAIL_FROM_NAME")
                ?? configuration["Mail:FromName"]
                ?? "TipsWall Ops";
        }

        public bool IsConfigured
            => !string.IsNullOrWhiteSpace(_host)
            && !string.IsNullOrWhiteSpace(_username)
            && !string.IsNullOrWhiteSpace(_password);

        public async Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
        {
            if (!IsConfigured)
            {
                _logger.LogWarning(
                    "SmtpEmailService skipped — MAIL_HOST/USERNAME/PASSWORD not set. Subject was {Subject}.",
                    subject);
                return;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_fromName, _from));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            using var client = new SmtpClient();
            try
            {
                await client.ConnectAsync(_host, _port, _security, ct);
                await client.AuthenticateAsync(_username, _password, ct);
                await client.SendAsync(message, ct);
                await client.DisconnectAsync(true, ct);
                _logger.LogInformation(
                    "SmtpEmailService delivered to {To} via {Host}:{Port}.", to, _host, _port);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "SmtpEmailService failed to deliver to {To} via {Host}:{Port}.", to, _host, _port);
                throw;
            }
        }
    }
}
