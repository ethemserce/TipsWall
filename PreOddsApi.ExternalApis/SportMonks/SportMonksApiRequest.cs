namespace PreOddsApi.ExternalApis.SportMonks
{
    public sealed class SportMonksApiRequest
    {
        private readonly List<KeyValuePair<string, string>> _queryParameters = new();

        private SportMonksApiRequest(string endpoint)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new ArgumentException("SportMonks endpoint is required.", nameof(endpoint));
            }

            Endpoint = endpoint.Trim();
        }

        public string Endpoint { get; }

        public bool ApplyDefaultPagination { get; private set; } = true;

        public int RequestDelayMs { get; private set; }

        public IReadOnlyCollection<KeyValuePair<string, string>> QueryParameters => _queryParameters;

        public static SportMonksApiRequest Create(string endpoint)
        {
            return new SportMonksApiRequest(endpoint);
        }

        public SportMonksApiRequest WithQueryParameter(string key, string? value)
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
            {
                return this;
            }

            _queryParameters.RemoveAll(item =>
                string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase));
            _queryParameters.Add(new KeyValuePair<string, string>(key.Trim(), value.Trim()));

            return this;
        }

        public SportMonksApiRequest WithQueryParameters(IEnumerable<KeyValuePair<string, string>>? queryParameters)
        {
            if (queryParameters == null)
            {
                return this;
            }

            foreach (var queryParameter in queryParameters)
            {
                WithQueryParameter(queryParameter.Key, queryParameter.Value);
            }

            return this;
        }

        public SportMonksApiRequest WithInclude(params string[] includes)
        {
            return WithJoinedQueryParameter("include", includes, ";");
        }

        public SportMonksApiRequest WithFilter(string filter, string? value)
        {
            if (string.IsNullOrWhiteSpace(filter) || string.IsNullOrWhiteSpace(value))
            {
                return this;
            }

            return WithQueryParameter("filters", $"{filter.Trim()}:{value.Trim()}");
        }

        public SportMonksApiRequest WithFilters(params string[] filters)
        {
            return WithJoinedQueryParameter("filters", filters, ";");
        }

        public SportMonksApiRequest WithTimezone(string timezone)
        {
            return WithQueryParameter("timezone", timezone);
        }

        public SportMonksApiRequest WithoutDefaultPagination()
        {
            ApplyDefaultPagination = false;
            return this;
        }

        public SportMonksApiRequest WithRequestDelayMs(int delayMs)
        {
            if (delayMs > 0)
                RequestDelayMs = delayMs;
            return this;
        }

        private SportMonksApiRequest WithJoinedQueryParameter(
            string key,
            IEnumerable<string> values,
            string separator)
        {
            var joinedValue = string.Join(separator,
                values.Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value.Trim()));

            return WithQueryParameter(key, joinedValue);
        }
    }
}
