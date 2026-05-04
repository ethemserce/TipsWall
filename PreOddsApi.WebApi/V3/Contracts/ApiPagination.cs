using System;
using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Contracts
{
    public sealed class ApiPagination
    {
        [JsonProperty("page")]
        public int Page { get; init; }

        [JsonProperty("per_page")]
        public int PerPage { get; init; }

        [JsonProperty("total")]
        public int Total { get; init; }

        [JsonProperty("total_pages")]
        public int TotalPages { get; init; }

        public static ApiPagination From(int page, int perPage, int total)
        {
            var totalPages = perPage > 0 ? (int)Math.Ceiling((double)total / perPage) : 0;
            return new ApiPagination
            {
                Page = page,
                PerPage = perPage,
                Total = total,
                TotalPages = totalPages
            };
        }
    }
}
