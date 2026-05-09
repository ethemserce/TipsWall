using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace PreOddsApi.WebApi.V3.Helpers
{
    /// <summary>
    /// Stamps every request with a correlation ID — incoming X-Correlation-Id
    /// is honoured if present, otherwise a fresh GUID is minted. The id is
    /// pushed into Serilog's LogContext so every log line emitted during the
    /// request carries it, and echoed back as a response header so clients
    /// (and aggregators) can stitch their side together.
    ///
    /// Mount this BEFORE UseRouting so even early-pipeline rejects (rate
    /// limiter 429s, etc.) get a correlation id in the response.
    /// </summary>
    public sealed class CorrelationIdMiddleware
    {
        public const string HeaderName = "X-Correlation-Id";

        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var incoming)
                && !string.IsNullOrWhiteSpace(incoming)
                    ? incoming.ToString()
                    : Guid.NewGuid().ToString("N");

            context.Items["CorrelationId"] = correlationId;
            context.Response.Headers[HeaderName] = correlationId;

            // Ride alongside the W3C trace so APM tools see one identifier.
            System.Diagnostics.Activity.Current?.AddTag("correlation_id", correlationId);

            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                await _next(context);
            }
        }
    }
}
