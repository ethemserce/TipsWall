using System;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace PreOddsApi.WebApi.Tests.V3.Integration
{
    /// <summary>
    /// In-process WebApi host pointed at the per-fixture Postgres container.
    /// Test code drives it via HttpClient — no real port binding, no network.
    /// </summary>
    public sealed class WebApiFactory : WebApplicationFactory<Program>
    {
        public WebApiFactory(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public string ConnectionString { get; }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.ConfigureHostConfiguration(cfg =>
            {
                cfg.AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string?>
                {
                    ["ConnectionStrings:PreOddsApiPostgresDb"] = ConnectionString,
                    ["DatabaseProvider"] = "postgresql",
                    // Strong JWT secret so the prod-mode guard doesn't trip.
                    ["Authentication:JwtSecret"] = "TEST_JWT_SECRET_AT_LEAST_32_CHARACTERS_LONG_xx",
                    ["Authentication:Issuer"] = "http://localhost",
                    ["Authentication:Audience"] = "http://localhost",
                });
            });

            // Setting environment to Development keeps swagger + dev CORS
            // available; tests use the regular pipeline otherwise.
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            Environment.SetEnvironmentVariable("PREODDS_POSTGRES_CONNECTION", ConnectionString);

            return base.CreateHost(builder);
        }
    }

    [Collection(PostgresCollection.Name)]
    public abstract class IntegrationTestBase : IAsyncLifetime
    {
        protected PostgresTestFixture Postgres { get; }
        protected WebApiFactory Factory { get; private set; } = null!;
        protected System.Net.Http.HttpClient Client { get; private set; } = null!;

        protected IntegrationTestBase(PostgresTestFixture postgres)
        {
            Postgres = postgres;
        }

        public virtual System.Threading.Tasks.Task InitializeAsync()
        {
            Factory = new WebApiFactory(Postgres.ConnectionString);
            Client = Factory.CreateClient();
            return System.Threading.Tasks.Task.CompletedTask;
        }

        public virtual System.Threading.Tasks.Task DisposeAsync()
        {
            Client.Dispose();
            return Factory.DisposeAsync().AsTask();
        }
    }
}
