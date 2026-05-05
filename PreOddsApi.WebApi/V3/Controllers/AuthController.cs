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
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public sealed class AuthController : ApiControllerBase
    {
        private const int TokenLifetimeSeconds = 86400;

        private readonly IUserIdentityService _identity;
        private readonly string _jwtSecret;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;

        public AuthController(IUserIdentityService identity, IConfiguration configuration)
        {
            _identity = identity;
            _jwtSecret = Environment.GetEnvironmentVariable("PREODDS_JWT_SECRET")
                ?? configuration["Authentication:JwtSecret"]
                ?? "CHANGE_ME_PREODDS_JWT_SECRET_32_CHARS_MINIMUM";
            _jwtIssuer = configuration["Authentication:Issuer"] ?? "http://localhost:28332";
            _jwtAudience = configuration["Authentication:Audience"] ?? "http://localhost:28332";
        }

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

            return OkResponse(new TokenDto
            {
                AccessToken = GenerateToken(user),
                TokenType = "Bearer",
                ExpiresIn = TokenLifetimeSeconds
            });
        }

        [HttpPost("signup")]
        public async Task<IActionResult> SignupAsync(
            [FromBody] SignupRequest request,
            CancellationToken ct)
        {
            var outcome = await _identity.SignupAsync(
                request.Username,
                request.Email,
                request.Password,
                request.DisplayName,
                ct);

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
            return Ok(ApiResponse<AuthResponseDto>.Ok(new AuthResponseDto
            {
                User = user,
                AccessToken = GenerateToken(user),
                TokenType = "Bearer",
                ExpiresIn = TokenLifetimeSeconds
            }));
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

        private string GenerateToken(UserDto user)
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
                expires: DateTime.UtcNow.AddSeconds(TokenLifetimeSeconds),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
