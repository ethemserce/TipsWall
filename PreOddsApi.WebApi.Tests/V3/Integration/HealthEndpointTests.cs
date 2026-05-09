using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace PreOddsApi.WebApi.Tests.V3.Integration
{
    /// <summary>
    /// Smoke test that exercises the full host: container boot →
    /// migrations applied → WebApi pipeline → health endpoint round-trip.
    /// If this test fails the integration scaffolding itself is broken;
    /// every subsequent integration test would also fail.
    /// </summary>
    public sealed class HealthEndpointTests : IntegrationTestBase
    {
        public HealthEndpointTests(PostgresTestFixture postgres) : base(postgres) { }

        [Fact]
        public async Task LiveEndpoint_ReturnsHealthy()
        {
            var response = await Client.GetAsync("/health/live");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ReadyEndpoint_ReturnsHealthyWhenDatabaseUp()
        {
            // Postgres is up via Testcontainers — sync_runs is empty so the
            // freshness check returns Healthy ("not yet created" branch is
            // also healthy). The composite must be 200.
            var response = await Client.GetAsync("/health/ready");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task PrometheusEndpoint_ServesOpenMetrics()
        {
            var response = await Client.GetAsync("/metrics");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            // Prometheus exposition format always begins with HELP/TYPE
            // comments — sanity-check we got the right content.
            Assert.Contains("# TYPE", body);
        }
    }
}
