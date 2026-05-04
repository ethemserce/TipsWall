using System;
using Microsoft.AspNetCore.Mvc;

namespace PreOddsApi.WebApi.V3.Contracts
{
    public class PagedRequest
    {
        private const int MaxPerPage = 200;
        private const int DefaultPerPage = 50;

        [FromQuery(Name = "page")]
        public int Page { get; set; } = 1;

        [FromQuery(Name = "per_page")]
        public int PerPage { get; set; } = DefaultPerPage;

        public int NormalizedPage => Math.Max(1, Page);

        public int NormalizedPerPage => Math.Clamp(PerPage, 1, MaxPerPage);

        public int Offset => (NormalizedPage - 1) * NormalizedPerPage;
    }
}
