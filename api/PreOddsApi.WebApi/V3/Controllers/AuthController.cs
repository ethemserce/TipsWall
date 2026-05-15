using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using PreOddsApi.ExternalApis.Notifications;
using PreOddsApi.WebApi.V3.Auth;
using PreOddsApi.WebApi.V3.Contracts;
using PreOddsApi.WebApi.V3.Data;
using PreOddsApi.WebApi.V3.Dtos;

namespace PreOddsApi.WebApi.V3.Controllers
{
    [EnableRateLimiting("auth")]
    public sealed class AuthController : ApiControllerBase
    {
        // 1h is enough for users to click an email link without keeping
        // the door open through a stolen mailbox archive.
        private static readonly TimeSpan PasswordResetLifetime = TimeSpan.FromHours(1);
        // Email verify is less time-sensitive — links commonly sit in
        // inboxes for a day or two before being clicked.
        private static readonly TimeSpan EmailVerifyLifetime = TimeSpan.FromHours(24);

        private readonly IUserIdentityService _identity;
        private readonly IRefreshTokenService _refreshTokens;
        private readonly IAccountTokenService _accountTokens;
        private readonly AuthOptions _authOptions;
        private readonly ISocialTokenVerifier _socialVerifier;
        private readonly ISocialIdentityService _socialIdentity;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IUserIdentityService identity,
            IRefreshTokenService refreshTokens,
            IAccountTokenService accountTokens,
            AuthOptions authOptions,
            ISocialTokenVerifier socialVerifier,
            ISocialIdentityService socialIdentity,
            IEmailService emailService,
            ILogger<AuthController> logger)
        {
            _identity = identity;
            _refreshTokens = refreshTokens;
            _accountTokens = accountTokens;
            _authOptions = authOptions;
            _socialVerifier = socialVerifier;
            _socialIdentity = socialIdentity;
            _emailService = emailService;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpPost("token")]
        public async Task<IActionResult> TokenAsync(
            [FromBody] LoginRequest request,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequestResponse("username and password are required.");

            var user = await _identity.AuthenticateAsync(request.Username, request.Password, ct);
            if (user == null)
                return Unauthorized(ApiResponse<object>.Fail(
                    ApiError.Codes.Unauthorized, "Invalid credentials."));

            var refresh = await _refreshTokens.IssueAsync(
                user.Id, GetUserAgent(), GetIpAddress(), ct);

            return OkResponse(new TokenDto
            {
                AccessToken = GenerateAccessToken(user),
                RefreshToken = refresh.RawToken,
                TokenType = "Bearer",
                ExpiresIn = _authOptions.AccessTokenLifetimeSeconds
            });
        }

        [AllowAnonymous]
        [HttpPost("signup")]
        public async Task<IActionResult> SignupAsync(
            [FromBody] SignupRequest request,
            CancellationToken ct)
        {
            var outcome = await _identity.SignupAsync(
                request.Username, request.Email, request.Password, request.DisplayName, ct);

            if (!outcome.Succeeded)
            {
                if (outcome.ErrorCode == SignupOutcome.ErrorCodes.UsernameTaken ||
                    outcome.ErrorCode == SignupOutcome.ErrorCodes.EmailTaken)
                {
                    return Conflict(ApiResponse<object>.Fail(
                        outcome.ErrorCode!, outcome.ErrorMessage ?? "Conflict."));
                }
                return BadRequestResponse(outcome.ErrorMessage ?? "Validation failed.");
            }

            var user = outcome.User!;
            var refresh = await _refreshTokens.IssueAsync(
                user.Id, GetUserAgent(), GetIpAddress(), ct);

            // Auto-send the email-verify link if the user gave a real email
            // address. Soft policy: the JWT still issues immediately so the
            // user can use the app; sensitive write paths (kupon kaydet,
            // market change) gate on `email_verified`. Fire-and-forget so a
            // mail provider hiccup doesn't fail the signup itself.
            if (!string.IsNullOrWhiteSpace(user.Email))
                await TrySendEmailVerificationLinkAsync(user.Id, user.Email!, ct);

            return Ok(ApiResponse<AuthResponseDto>.Ok(new AuthResponseDto
            {
                User = user,
                AccessToken = GenerateAccessToken(user),
                RefreshToken = refresh.RawToken,
                TokenType = "Bearer",
                ExpiresIn = _authOptions.AccessTokenLifetimeSeconds
            }));
        }

        [AllowAnonymous]
        [HttpPost("social-signin")]
        public async Task<IActionResult> SocialSignInAsync(
            [FromBody] SocialSignInRequest request,
            CancellationToken ct)
        {
            // Both Apple and Google return a signed JWT (id_token). The
            // mobile client posts it here; the server verifies signature
            // + audience against the configured OAuth client IDs, then
            // either finds the existing identity or creates a new user.
            if (request == null ||
                string.IsNullOrWhiteSpace(request.Provider) ||
                string.IsNullOrWhiteSpace(request.IdToken))
            {
                return BadRequestResponse("provider and id_token are required.");
            }

            var provider = request.Provider.Trim().ToLowerInvariant();
            SocialIdentityProfile profile;
            try
            {
                profile = provider switch
                {
                    "apple" => await _socialVerifier.VerifyAppleAsync(request.IdToken, ct),
                    "google" => await _socialVerifier.VerifyGoogleAsync(request.IdToken, ct),
                    _ => throw new SocialTokenInvalidException(
                        $"Unsupported provider '{provider}'."),
                };
            }
            catch (SocialTokenInvalidException ex)
            {
                return Unauthorized(ApiResponse<object>.Fail(
                    ApiError.Codes.Unauthorized, ex.Message));
            }

            var user = await _socialIdentity.UpsertFromProviderAsync(
                provider, profile.Subject, profile.Email, profile.Name, ct);

            var refresh = await _refreshTokens.IssueAsync(
                user.Id, GetUserAgent(), GetIpAddress(), ct);

            return Ok(ApiResponse<AuthResponseDto>.Ok(new AuthResponseDto
            {
                User = user,
                AccessToken = GenerateAccessToken(user),
                RefreshToken = refresh.RawToken,
                TokenType = "Bearer",
                ExpiresIn = _authOptions.AccessTokenLifetimeSeconds
            }));
        }

        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshAsync(
            [FromBody] RefreshTokenRequest request,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return BadRequestResponse("refresh_token is required.");

            var result = await _refreshTokens.RotateAsync(
                request.RefreshToken, GetUserAgent(), GetIpAddress(), ct);

            if (!result.Succeeded || result.NewToken == null)
                return Unauthorized(ApiResponse<object>.Fail(
                    ApiError.Codes.Unauthorized,
                    $"Refresh token invalid: {result.FailureReason ?? "unknown"}."));

            var user = await _identity.GetByIdAsync(result.UserId, ct)
                ?? new UserDto { Id = result.UserId };

            return OkResponse(new TokenDto
            {
                AccessToken = GenerateAccessToken(user),
                RefreshToken = result.NewToken.RawToken,
                TokenType = "Bearer",
                ExpiresIn = _authOptions.AccessTokenLifetimeSeconds
            });
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> LogoutAsync(
            [FromBody] RefreshTokenRequest request,
            CancellationToken ct)
        {
            // Both an access token (Authorize) AND the refresh token are
            // required — the access token proves identity, the refresh token
            // is the session being revoked. This stops a leaked refresh token
            // alone from being weaponised against a logged-in session.
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return BadRequestResponse("refresh_token is required.");

            await _refreshTokens.RevokeAsync(request.RefreshToken, "logout", ct);
            return OkResponse(new { revoked = true });
        }

        // ---------- Password reset ------------------------------------

        [AllowAnonymous]
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPasswordAsync(
            [FromBody] ForgotPasswordRequest request,
            CancellationToken ct)
        {
            // Always return 200 regardless of whether the account exists.
            // Leaking existence via 404 / timing is a privacy regression.
            // The token (when issued) is logged out so an operator can
            // surface it to the email pipeline; in production this hands
            // off to your email provider (SES / Postmark / Sendgrid).
            var userId = await _identity.FindUserIdByEmailOrUsernameAsync(
                request.EmailOrUsername ?? string.Empty, ct);

            string? rawToken = null;
            if (userId.HasValue)
            {
                var issued = await _accountTokens.IssueAsync(
                    userId.Value, AccountTokenPurpose.PasswordReset, PasswordResetLifetime, ct);
                rawToken = issued.RawToken;
                // Password-reset email send + matching web page is a
                // separate follow-up — would 404 today because there's no
                // /auth/reset-password browser entry point yet. The token
                // is still surfaced via dev_token in non-prod so an e2e
                // test can redeem it; in-app reset can wire in later.
            }

            // The token is returned in the body in non-Production environments
            // so the mobile dev / e2e tester can redeem it without a real
            // email pipeline. Production builds suppress it.
            var includeTokenInBody = string.Equals(
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                "Development",
                StringComparison.OrdinalIgnoreCase);

            return OkResponse(includeTokenInBody && rawToken != null
                ? new { sent = true, dev_token = rawToken }
                : (object)new { sent = true });
        }

        [AllowAnonymous]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPasswordAsync(
            [FromBody] ResetPasswordRequest request,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.Token))
                return BadRequestResponse("token is required.");
            if (string.IsNullOrEmpty(request.NewPassword) || request.NewPassword.Length < 8)
                return BadRequestResponse("new_password must be at least 8 characters.");

            var redemption = await _accountTokens.ConsumeAsync(
                request.Token, AccountTokenPurpose.PasswordReset, ct);
            if (!redemption.Succeeded)
                return BadRequestResponse("Invalid or expired token.");

            var ok = await _identity.ResetPasswordAsync(redemption.UserId, request.NewPassword, ct);
            if (!ok)
                return BadRequestResponse("Could not update password.");

            return OkResponse(new { reset = true });
        }

        // ---------- Email verification --------------------------------

        [Authorize]
        [HttpPost("request-email-verification")]
        public async Task<IActionResult> RequestEmailVerificationAsync(CancellationToken ct)
        {
            var uid = User.FindFirst("uid")?.Value;
            if (!Guid.TryParse(uid, out var userId))
                return Unauthorized(ApiResponse<object>.Fail(
                    ApiError.Codes.Unauthorized, "Invalid token."));

            // Need the email address to actually send the link; the JWT
            // carries one but the DB row is the source of truth (the user
            // may have changed it since the token was issued).
            var user = await _identity.GetByIdAsync(userId, ct);
            if (user == null || string.IsNullOrWhiteSpace(user.Email))
                return BadRequestResponse("Account has no email on file.");

            var rawToken = await TrySendEmailVerificationLinkAsync(userId, user.Email!, ct);

            var includeTokenInBody = string.Equals(
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                "Development",
                StringComparison.OrdinalIgnoreCase);

            return OkResponse(includeTokenInBody && rawToken != null
                ? new { sent = true, dev_token = rawToken }
                : (object)new { sent = true });
        }

        [AllowAnonymous]
        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmailAsync(
            [FromBody] VerifyEmailRequest request,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.Token))
                return BadRequestResponse("token is required.");

            var redemption = await _accountTokens.ConsumeAsync(
                request.Token, AccountTokenPurpose.EmailVerify, ct);
            if (!redemption.Succeeded)
                return BadRequestResponse("Invalid or expired token.");

            await _identity.MarkEmailVerifiedAsync(redemption.UserId, ct);
            return OkResponse(new { verified = true });
        }

        /// <summary>
        /// Browser-friendly entry point that the email "verify" link
        /// targets. Consumes the token and renders a minimal HTML
        /// confirmation page — the mobile app then notices the flipped
        /// flag on the next /auth/me refresh. We use GET here (links in
        /// emails can't POST without a form), so the token is single-
        /// use server-side which prevents accidental re-runs from
        /// confused inboxes that prefetch links.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("verify-email")]
        public async Task<IActionResult> VerifyEmailLinkAsync(
            [FromQuery(Name = "token")] string? token,
            CancellationToken ct)
        {
            string title;
            string message;
            if (string.IsNullOrWhiteSpace(token))
            {
                title = "Bağlantı eksik";
                message = "Doğrulama bağlantısı geçersiz görünüyor. Uygulamadan tekrar gönder.";
            }
            else
            {
                var redemption = await _accountTokens.ConsumeAsync(
                    token, AccountTokenPurpose.EmailVerify, ct);
                if (!redemption.Succeeded)
                {
                    title = "Bağlantı süresi doldu";
                    message = "Bu bağlantı kullanılmış veya 24 saat geçmiş olabilir. Uygulamadan yeni bir bağlantı iste.";
                }
                else
                {
                    await _identity.MarkEmailVerifiedAsync(redemption.UserId, ct);
                    title = "Email doğrulandı";
                    message = "Hesabın onaylandı. Uygulamaya geri dönebilirsin.";
                }
            }
            var html =
                "<!doctype html><html lang=\"tr\"><head><meta charset=\"utf-8\"/>" +
                "<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"/>" +
                $"<title>TipsWall — {title}</title>" +
                "<style>body{font-family:-apple-system,BlinkMacSystemFont,Segoe UI,Roboto,sans-serif;" +
                "background:#0b0b0e;color:#eaeaea;margin:0;display:flex;align-items:center;" +
                "justify-content:center;min-height:100vh;padding:24px;text-align:center}" +
                ".card{background:#15161a;border:1px solid #2a2c33;border-radius:14px;" +
                "padding:32px 28px;max-width:420px}h1{font-size:20px;margin:0 0 12px}" +
                "p{font-size:14px;line-height:1.5;color:#bdbdc4;margin:0}" +
                ".brand{font-size:12px;letter-spacing:1.5px;color:#8c8d96;margin-top:24px}" +
                "</style></head><body><div class=\"card\"><h1>" + title + "</h1>" +
                "<p>" + message + "</p><div class=\"brand\">TIPSWALL</div></div></body></html>";
            return Content(html, "text/html; charset=utf-8");
        }

        private async Task<string?> TrySendEmailVerificationLinkAsync(
            Guid userId, string email, CancellationToken ct)
        {
            try
            {
                var issued = await _accountTokens.IssueAsync(
                    userId, AccountTokenPurpose.EmailVerify, EmailVerifyLifetime, ct);
                var verifyUrl = $"{_authOptions.Issuer.TrimEnd('/')}/api/v3/auth/verify-email?token={Uri.EscapeDataString(issued.RawToken)}";
                var body =
                    "Merhaba,\n\n" +
                    "TipsWall hesabını doğrulamak için aşağıdaki bağlantıya tıkla. " +
                    $"Bağlantı 24 saat geçerlidir.\n\n{verifyUrl}\n\n" +
                    "Bu hesabı sen oluşturmadıysan bu maili görmezden gelebilirsin.\n\n" +
                    "TipsWall";
                await _emailService.SendAsync(email, "TipsWall — Email doğrulama", body, ct);
                return issued.RawToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send email-verify link to {Email}.", email);
                return null;
            }
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me(CancellationToken ct)
        {
            var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            var uid = User.FindFirst("uid")?.Value;
            var email = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
            var tier = User.FindFirst("tier")?.Value ?? "free";

            // Read email_verified from the DB so a freshly clicked link
            // surfaces *immediately* — the JWT claim is up to 15 min
            // stale because access tokens are not invalidated mid-life.
            bool emailVerified = false;
            if (Guid.TryParse(uid, out var userId))
            {
                var user = await _identity.GetByIdAsync(userId, ct);
                if (user != null)
                    emailVerified = user.EmailVerified;
            }

            return OkResponse(new
            {
                username = sub,
                uid,
                email,
                tier,
                email_verified = emailVerified
            });
        }

        [Authorize]
        [HttpDelete("me")]
        public async Task<IActionResult> DeleteAccountAsync(
            [FromBody] DeleteAccountRequest? request,
            CancellationToken ct)
        {
            // Apple & Google both require an in-app account deletion path.
            // Soft-delete scrubs PII immediately (email/username/etc.) and
            // marks the row 'deleted'; a nightly purge hard-removes after
            // 30 days. Every refresh token for the user is revoked too so
            // existing sessions on other devices are killed instantly.
            var uid = User.FindFirst("uid")?.Value;
            if (!Guid.TryParse(uid, out var userId))
                return Unauthorized(ApiResponse<object>.Fail(
                    ApiError.Codes.Unauthorized, "Invalid token."));

            var ok = await _identity.SoftDeleteAccountAsync(
                userId, request?.Reason, ct);
            if (!ok)
                return NotFoundResponse("Account not found or already deleted.");

            await _refreshTokens.RevokeAllForUserAsync(
                userId, "account-deleted", ct);

            return OkResponse(new { deleted = true });
        }

        private string GenerateAccessToken(UserDto user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_authOptions.JwtSecret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new System.Collections.Generic.List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Username ?? user.Id.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new("uid", user.Id.ToString()),
                // Membership tier — drives mobile gating + server-side
                // feature filters. Stamped at token issue time, so an
                // upgrade only takes effect after the next refresh (15m
                // grace). Acceptable; full enforcement still hits the DB.
                new("tier", user.Tier),
                // email_verified is also stamped at issue time so mobile
                // can hide the "verify your email" banner without a /me
                // round trip. Refresh after verifying picks up the new
                // value within 15 min.
                new("email_verified", user.EmailVerified ? "true" : "false")
            };

            if (!string.IsNullOrWhiteSpace(user.Email))
                claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));

            var token = new JwtSecurityToken(
                issuer: _authOptions.Issuer,
                audience: _authOptions.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddSeconds(_authOptions.AccessTokenLifetimeSeconds),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string? GetUserAgent()
        {
            var ua = Request.Headers.UserAgent.ToString();
            return string.IsNullOrWhiteSpace(ua) ? null : ua;
        }

        private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
