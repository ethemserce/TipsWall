using PreOddsApi.WebApi.V3.Contracts;
using Xunit;

namespace PreOddsApi.WebApi.Tests.V3.Contracts
{
    public class PagedRequestTests
    {
        [Theory]
        [InlineData(0, 1)]
        [InlineData(-5, 1)]
        [InlineData(1, 1)]
        [InlineData(7, 7)]
        public void NormalizedPage_clamps_to_at_least_one(int page, int expected)
        {
            var request = new PagedRequest { Page = page };
            Assert.Equal(expected, request.NormalizedPage);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(-50, 1)]
        [InlineData(50, 50)]
        [InlineData(199, 199)]
        [InlineData(200, 200)]
        [InlineData(500, 200)]
        public void NormalizedPerPage_clamps_between_one_and_two_hundred(int perPage, int expected)
        {
            var request = new PagedRequest { PerPage = perPage };
            Assert.Equal(expected, request.NormalizedPerPage);
        }

        [Theory]
        [InlineData(1, 50, 0)]
        [InlineData(2, 50, 50)]
        [InlineData(3, 25, 50)]
        [InlineData(0, 50, 0)]
        public void Offset_uses_normalized_values(int page, int perPage, int expectedOffset)
        {
            var request = new PagedRequest { Page = page, PerPage = perPage };
            Assert.Equal(expectedOffset, request.Offset);
        }
    }
}
