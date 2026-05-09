using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PreOddsApi.WebApi.V3.Contracts;
using PreOddsApi.WebApi.V3.Data;

namespace PreOddsApi.WebApi.V3.Controllers
{
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [Route("api/v3/contact")]
    public sealed class ContactV3Controller : ApiControllerBase
    {
        private readonly IAppSchemaService _appSchema;

        public ContactV3Controller(IAppSchemaService appSchema)
        {
            _appSchema = appSchema;
        }

        [HttpPost]
        public async Task<IActionResult> SubmitAsync(
            [FromBody] ContactMessageRequest request,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.Name) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequestResponse("name, email and message are required.");
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers.UserAgent.ToString();

            var id = await _appSchema.SubmitContactMessageAsync(
                request.Name,
                request.Email,
                request.Subject,
                request.Message,
                request.Locale,
                ipAddress,
                string.IsNullOrWhiteSpace(userAgent) ? null : userAgent,
                ct);

            return OkResponse(new { id });
        }
    }
}
