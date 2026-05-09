using System.Net;

namespace PreOddsApi.ExternalApis.SportMonks
{
    public sealed class SportMonksApiException : Exception
    {
        public SportMonksApiException(
            string message,
            HttpStatusCode statusCode,
            string endpoint,
            string? responseBody = null)
            : base(message)
        {
            StatusCode = statusCode;
            Endpoint = endpoint;
            ResponseBody = responseBody;
        }

        public HttpStatusCode StatusCode { get; }

        public string Endpoint { get; }

        public string? ResponseBody { get; }
    }
}
