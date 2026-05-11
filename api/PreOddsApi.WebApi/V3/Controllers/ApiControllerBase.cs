using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PreOddsApi.WebApi.V3.Contracts;

namespace PreOddsApi.WebApi.V3.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/v3/[controller]")]
    public abstract class ApiControllerBase : ControllerBase
    {
        protected Guid? GetUserId()
        {
            var uid = User.FindFirst("uid")?.Value;
            return Guid.TryParse(uid, out var id) ? id : null;
        }

        /// <summary>
        /// Membership tier of the calling user. Returns "guest" when no
        /// JWT is present (anonymous request on a [AllowAnonymous]
        /// endpoint), otherwise the tier claim baked into the token
        /// ("free" or "premium"). Use this to gate response detail on
        /// public read endpoints — no [Authorize] required, but the
        /// response can be enriched when the caller is logged in.
        /// </summary>
        protected string GetTier()
        {
            if (!(User?.Identity?.IsAuthenticated ?? false))
                return "guest";
            return User.FindFirst("tier")?.Value ?? "free";
        }

        protected bool IsPremium() => GetTier() == "premium";

        protected bool IsRegistered()
        {
            var t = GetTier();
            return t == "free" || t == "premium";
        }

        protected IActionResult OkResponse<T>(T data)
            => Ok(ApiResponse<T>.Ok(data));

        protected IActionResult OkPagedResponse<T>(
            IReadOnlyList<T> data,
            int page,
            int perPage,
            int total)
            => Ok(ApiResponse<IReadOnlyList<T>>.OkPaged(
                data,
                ApiPagination.From(page, perPage, total)));

        protected IActionResult OkPagedObjectResponse<T>(
            T data,
            int page,
            int perPage,
            int total)
            => Ok(ApiResponse<T>.OkPaged(
                data,
                ApiPagination.From(page, perPage, total)));

        protected IActionResult NotFoundResponse(string message)
            => NotFound(ApiResponse<object>.Fail(ApiError.Codes.NotFound, message));

        protected IActionResult BadRequestResponse(string message)
            => BadRequest(ApiResponse<object>.Fail(ApiError.Codes.BadRequest, message));

        protected IActionResult InternalErrorResponse(string message)
            => StatusCode(500, ApiResponse<object>.Fail(ApiError.Codes.InternalError, message));
    }
}
