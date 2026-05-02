using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using PreOddsApi.WebApi.Models.Token;
using PreOddsApi.BusinessLayer.Abstract;
using AutoMapper;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using PreOddsApi.Utils;
using Microsoft.IdentityModel.JsonWebTokens;

namespace PreOddsApi.WebApi.Controllers
{
    [Authorize]
    [Produces("application/json")]
    [Route("api")]
    public class PrdTokenController : Controller
    {
        //private readonly IMapper _mapper;
        private readonly IPrdUserService _prdUserService;

        public PrdTokenController(IPrdUserService prdUserService, IMapper mapper)
        {
            _prdUserService = prdUserService;
           //_mapper = mapper;
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("prdToken")]
        public IActionResult Post([FromBody]LoginRequestViewModel request)
        {
            if (ModelState.IsValid)
            {
                //if(!SecurePasswordHasher.Verify(request.Password, SecurePasswordHasher.Hash(request.Password)))
                //{
                //    return Unauthorized();
                //}

                var user = _prdUserService.GetUser(request.Username, SecurePasswordHasher.Hash(request.Password));
                if (user == null)
                {
                    return Unauthorized();
                }

                var claims = new[] {
                    new Claim(JwtRegisteredClaimNames.Sub, request.Username),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                var jwtSecret = Environment.GetEnvironmentVariable("PREODDS_JWT_SECRET") ?? "CHANGE_ME_PREODDS_JWT_SECRET_32_CHARS_MINIMUM";

                var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
                    issuer: "http://localhost:28332",
                    audience: "http://localhost:28332",
                    claims: claims,
                    expires: DateTime.UtcNow.AddDays(1),
                    notBefore: DateTime.UtcNow,
                    signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)), SecurityAlgorithms.HmacSha256)
                    );

                return Ok(new { token = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token) });
            }

            return BadRequest();
        }
    }
}
