using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PreOddsApi.Worker
{
    /// <summary>
    /// Tiny in-process HTTP listener that answers /health/live and
    /// /health/ready for K8s liveness/readiness probes. Workers don't
    /// host Kestrel, but the orchestrator still needs *something* to
    /// hit — without this the probe falls back to "process exists",
    /// which says nothing about whether the worker loop is healthy.
    ///
    /// The port comes from WORKER_HEALTH_PORT (default 8081); set
    /// distinct ports per worker container in docker-compose so they
    /// don't collide on the host network. When the env var is "0" or
    /// unparseable the endpoint is silently disabled — useful for
    /// tests + local dev where multiple workers share one machine.
    /// </summary>
    public sealed class WorkerHealthEndpoint : BackgroundService
    {
        private readonly string _serviceName;
        private readonly ILogger<WorkerHealthEndpoint> _logger;
        private readonly int _port;

        public WorkerHealthEndpoint(string serviceName, ILogger<WorkerHealthEndpoint> logger)
        {
            _serviceName = serviceName;
            _logger = logger;
            var raw = Environment.GetEnvironmentVariable("WORKER_HEALTH_PORT");
            _port = int.TryParse(raw, out var p) ? p : 8081;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_port <= 0)
            {
                _logger.LogInformation("Health endpoint disabled (WORKER_HEALTH_PORT={Port}).", _port);
                return;
            }

            using var listener = new HttpListener();
            // Loopback only. The "+" form would catch every host header
            // but Windows requires an admin urlacl reservation for it
            // (HttpListenerException: Access is denied), and our K8s
            // probe contract is "the kubelet runs `curl localhost:port`
            // from inside the container" — same loopback either way.
            listener.Prefixes.Add($"http://localhost:{_port}/");
            try
            {
                listener.Start();
            }
            catch (HttpListenerException ex)
            {
                // Most common cause: another worker on the same host got
                // 8081 first, OR the user's machine doesn't have urlacl
                // permissions. Log loud and degrade gracefully.
                _logger.LogWarning(ex,
                    "Could not bind health endpoint on port {Port}; probes will fail.",
                    _port);
                return;
            }

            _logger.LogInformation(
                "{Service} health endpoint listening on http://0.0.0.0:{Port}/health/live",
                _serviceName, _port);

            // The listener loop quietly stops when the host shuts down.
            // GetContextAsync respects the linked cancellation via Stop()
            // below, which throws ObjectDisposedException — caught silently.
            stoppingToken.Register(() =>
            {
                try { listener.Stop(); } catch { /* shutting down */ }
            });

            while (!stoppingToken.IsCancellationRequested)
            {
                HttpListenerContext context;
                try
                {
                    context = await listener.GetContextAsync().ConfigureAwait(false);
                }
                catch (HttpListenerException) { break; }
                catch (ObjectDisposedException) { break; }

                _ = Task.Run(() => HandleAsync(context), stoppingToken);
            }
        }

        private async Task HandleAsync(HttpListenerContext context)
        {
            var path = context.Request.Url?.AbsolutePath ?? "/";
            try
            {
                if (path == "/health/live" || path == "/health/ready")
                {
                    var body = $"{{\"status\":\"healthy\",\"service\":\"{_serviceName}\"}}";
                    var bytes = Encoding.UTF8.GetBytes(body);
                    context.Response.StatusCode = 200;
                    context.Response.ContentType = "application/json";
                    await context.Response.OutputStream.WriteAsync(bytes);
                }
                else
                {
                    context.Response.StatusCode = 404;
                }
            }
            catch
            {
                // Best-effort — a probe that times out will retry.
            }
            finally
            {
                try { context.Response.Close(); } catch { /* swallow */ }
            }
        }
    }
}
