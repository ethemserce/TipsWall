using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
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

        public AuthController(
            IUserIdentityService identity,
            IRefreshTokenService refreshTokens,
            IAccountTokenService accountTokens,
            AuthOptions authOptions)
        {
            _identity = identity;
            _refreshTokens = refreshTokens;
            _accountTokens = accountTokens;
            _authOptions = authOptions;
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

            var issued = await _accountTokens.IssueAsync(
                userId, AccountTokenPurpose.EmailVerify, EmailVerifyLifetime, ct);

            var includeTokenInBody = string.Equals(
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                "Development",
                StringComparison.OrdinalIgnoreCase);

            return OkResponse(includeTokenInBody
                ? new { sent = true, dev_token = issued.RawToken }
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

        [Authorize]
        [HttpGet("me")]
        public IActionResult Me()
        {
            var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            var uid = User.FindFirst("uid")?.Value;
            var email = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value;

            return OkResponse(new
            {
                username = sub,
                uid,
                email
            });
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
                new("tier", user.Tier)
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
