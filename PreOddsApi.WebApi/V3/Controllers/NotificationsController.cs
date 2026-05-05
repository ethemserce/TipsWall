using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PreOddsApi.WebApi.V3.Contracts;
using PreOddsApi.WebApi.V3.Data;

namespace PreOddsApi.WebApi.V3.Controllers
{
    public sealed class NotificationsController : ApiControllerBase
    {
        private readonly IUserDataService _userData;

        public NotificationsController(IUserDataService userData)
        {
            _userData = userData;
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync(
            [FromQuery(Name = "status")] string? status,
            [FromQuery] PagedRequest paging,
            CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(ApiResponse<object>.Fail(
                    ApiError.Codes.Unauthorized, "Token is missing user id."));

            var (items, total) = await _userData.GetNotificationsAsync(
                userId.Value, status, paging.NormalizedPage, paging.NormalizedPerPage, ct);

            return OkPagedResponse(items, paging.NormalizedPage, paging.NormalizedPerPage, total);
        }

        [HttpPost("{id:guid}/read")]
        public async Task<IActionResult> MarkReadAsync(Guid id, CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(ApiResponse<object>.Fail(
                    ApiError.Codes.Unauthorized, "Token is missing user id."));

            var notification = await _userData.MarkNotificationReadAsync(userId.Value, id, ct);
            if (notification == null)
                return NotFoundResponse($"Notification {id} not found or already read.");

            return OkResponse(notification);
        }
    }
}
