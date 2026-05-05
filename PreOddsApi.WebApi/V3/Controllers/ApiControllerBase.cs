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

        protected IActionResult NotFoundResponse(string message)
            => NotFound(ApiResponse<object>.Fail(ApiError.Codes.NotFound, message));

        protected IActionResult BadRequestResponse(string message)
            => BadRequest(ApiResponse<object>.Fail(ApiError.Codes.BadRequest, message));

        protected IActionResult InternalErrorResponse(string message)
            => StatusCode(500, ApiResponse<object>.Fail(ApiError.Codes.InternalError, message));
    }
}
