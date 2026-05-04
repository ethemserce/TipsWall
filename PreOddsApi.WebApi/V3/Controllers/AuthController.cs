using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PreOddsApi.BusinessLayer.Abstract;
using PreOddsApi.WebApi.V3.Contracts;
using PreOddsApi.WebApi.V3.Dtos;

namespace PreOddsApi.WebApi.V3.Controllers
{
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public sealed class AuthController : ApiControllerBase
    {
        private const int TokenLifetimeSeconds = 86400;

        private readonly IPrdUserService _userService;
        private readonly string _jwtSecret;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;

        public AuthController(IPrdUserService userService, IConfiguration configuration)
        {
            _userService = userService;
            _jwtSecret = Environment.GetEnvironmentVariable("PREODDS_JWT_SECRET")
                ?? configuration["Authentication:JwtSecret"]
                ?? "CHANGE_ME_PREODDS_JWT_SECRET_32_CHARS_MINIMUM";
            _jwtIssuer = configuration["Authentication:Issuer"] ?? "http://localhost:28332";
            _jwtAudience = configuration["Authentication:Audience"] ?? "http://localhost:28332";
        }

        [HttpPost("token")]
        public IActionResult Token([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequestResponse("username and password are required.");

            var user = _userService.GetUser(request.Username.Trim(), request.Password);
            if (user == null)
                return Unauthorized(ApiResponse<object>.Fail(
                    ApiError.Codes.Unauthorized, "Invalid credentials."));

            var token = GenerateToken(user.NickName ?? request.Username.Trim());

            return OkResponse(new TokenDto
            {
                AccessToken = token,
                TokenType = "Bearer",
                ExpiresIn = TokenLifetimeSeconds
            });
        }

        private string GenerateToken(string username)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

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
