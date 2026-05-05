using PreOddsApi.WebApi.V3.Contracts;
using Xunit;

namespace PreOddsApi.WebApi.Tests.V3.Contracts
{
    public class ApiResponseTests
    {
        [Fact]
        public void Ok_returns_success_with_data_and_no_error()
        {
            var response = ApiResponse<string>.Ok("hello");

            Assert.True(response.Success);
            Assert.Equal("hello", response.Data);
            Assert.Null(response.Error);
            Assert.Null(response.Pagination);
        }

        [Fact]
        public void OkPaged_returns_success_with_data_and_pagination()
        {
            var pagination = ApiPagination.From(2, 50, 250);
            var response = ApiResponse<int[]>.OkPaged(new[] { 1, 2, 3 }, pagination);

            Assert.True(response.Success);
            Assert.NotNull(response.Pagination);
            Assert.Equal(2, response.Pagination!.Page);
            Assert.Equal(50, response.Pagination.PerPage);
            Assert.Equal(250, response.Pagination.Total);
            Assert.Null(response.Error);
        }

        [Fact]
        public void Fail_returns_failure_with_error_and_no_data()
        {
            var response = ApiResponse<object>.Fail(ApiError.Codes.NotFound, "Missing.");

            Assert.False(response.Success);
            Assert.NotNull(response.Error);
            Assert.Equal(ApiError.Codes.NotFound, response.Error!.Code);
            Assert.Equal("Missing.", response.Error.Message);
            Assert.Null(response.Data);
        }
    }
}
