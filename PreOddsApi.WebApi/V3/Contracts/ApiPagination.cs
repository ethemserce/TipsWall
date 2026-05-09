using System;

namespace PreOddsApi.WebApi.V3.Contracts
{
    public sealed class ApiPagination
    {
        public int Page { get; init; }

        public int PerPage { get; init; }

        public int Total { get; init; }

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
