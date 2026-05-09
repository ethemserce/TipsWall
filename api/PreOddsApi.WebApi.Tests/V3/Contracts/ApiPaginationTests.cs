using PreOddsApi.WebApi.V3.Contracts;
using Xunit;

namespace PreOddsApi.WebApi.Tests.V3.Contracts
{
    public class ApiPaginationTests
    {
        [Theory]
        [InlineData(1, 50, 250, 5)]
        [InlineData(1, 50, 251, 6)]
        [InlineData(1, 50, 0, 0)]
        [InlineData(1, 50, 49, 1)]
        [InlineData(1, 50, 50, 1)]
        public void From_calculates_total_pages_correctly(
            int page, int perPage, int total, int expectedTotalPages)
        {
            var pagination = ApiPagination.From(page, perPage, total);

            Assert.Equal(page, pagination.Page);
            Assert.Equal(perPage, pagination.PerPage);
            Assert.Equal(total, pagination.Total);
            Assert.Equal(expectedTotalPages, pagination.TotalPages);
        }

        [Fact]
        public void From_returns_zero_total_pages_when_per_page_is_zero()
        {
            var pagination = ApiPagination.From(1, 0, 100);

            Assert.Equal(0, pagination.TotalPages);
        }
    }
}
