using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PreOddsApi.WebApi.V3.Contracts;
using PreOddsApi.WebApi.V3.Data;
using PreOddsApi.WebApi.V3.Dtos;

namespace PreOddsApi.WebApi.V3.Controllers
{
    [EnableRateLimiting("auth")]
    public sealed class AuthController : ApiControllerBase
    {
        private const int AccessTokenLifetimeSeconds = 86400;

        private readonly IUserIdentityService _identity;
        private readonly IRefreshTokenService _refreshTokens;
        private readonly string _jwtSecret;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;

        public AuthController(
            IUserIdentityService identity,
            IRefreshTokenService refreshTokens,
            IConfiguration configuration)
        {
            _identity = identity;
            _refreshTokens = refreshTokens;
            _jwtSecret = Environment.GetEnvironmentVariable("PREODDS_JWT_SECRET")
                ?? configuration["Authentication:JwtSecret"]
                ?? "CHANGE_ME_PREODDS_JWT_SECRET_32_CHARS_MINIMUM";
            _jwtIssuer = configuration["Authentication:Issuer"] ?? "http://localhost:28332";
            _jwtAudience = configuration["Authentication:Audience"] ?? "http://localhost:28332";
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
                ExpiresIn = AccessTokenLifetimeSeconds
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
                ExpiresIn = AccessTokenLifetimeSeconds
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
                ExpiresIn = AccessTokenLifetimeSeconds
            });
        }

        [AllowAnonymous]
        [HttpPost("logout")]
        public async Task<IActionResult> LogoutAsync(
            [FromBody] RefreshTokenRequest request,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return BadRequestResponse("refresh_token is required.");

            await _refreshTokens.RevokeAsync(request.RefreshToken, "logout", ct);
            return OkResponse(new { revoked = true });
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
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new System.Collections.Generic.List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Username ?? user.Id.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new("uid", user.Id.ToString())
            };

            if (!string.IsNullOrWhiteSpace(user.Email))
                claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));

            var token = new JwtSecurityToken(
                issuer: _jwtIssuer,
                audience: _jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddSeconds(AccessTokenLifetimeSeconds),
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
