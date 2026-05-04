# Task 022 - Runtime Config and Security Cleanup

## Goal

Make the current SportMonks/PostgreSQL runtime safer to run locally and easier to move toward production by removing secret-prone defaults, making PostgreSQL the default database provider, and reducing legacy worker dependencies that kept the old EF/MySQL path alive.

## Scope

- PostgreSQL becomes the default provider when `DatabaseProvider` is not set.
- PostgreSQL connection strings can be supplied through `PREODDS_POSTGRES_CONNECTION`.
- EF Core sensitive data logging is disabled by default and can only be enabled with:
  - `PREODDS_EF_SENSITIVE_LOGGING=true`
  - or `Database:EnableSensitiveDataLogging=true`
- WebApi JWT authentication middleware is added to the request pipeline.
- WebApi JWT issuer, audience, and secret can be configured through `Authentication:*` settings or `PREODDS_JWT_SECRET`.
- Worker appsettings examples use the modern `SportMonks` section and do not include token-style legacy placeholders.
- Core worker example points at the SportMonks `core` API segment.
- Unused legacy odds/core worker AutoMapper and EF/MySQL insert code is removed.

## Changed Areas

- `PreOddsApi.DataLayer`
- `PreOddsApi.ExternalApis`
- `PreOddsApi.WebApi`
- `PreOddsApi.Worker/SportMonks/SportMonks.Core`
- `PreOddsApi.Worker/SportMonks/SportMonks.Football.Fixture`
- `PreOddsApi.Worker/SportMonks/SportMonks.Odds`

## Out of Scope

- Replacing all existing WebApi controllers with new PostgreSQL read APIs.
- Removing the legacy DataLayer completely.
- Removing all `apiKey=1` route checks.
- Running live SportMonks imports.
- Changing production hosting or deployment topology.

## Acceptance Tests

1. `dotnet restore PreOddsApi.sln` completes successfully.
2. `dotnet build PreOddsApi.sln --no-restore` completes with zero errors.
3. Repository-tracked config examples do not contain a real SportMonks token.
4. WebApi pipeline calls `UseAuthentication()` before `UseAuthorization()`.
5. `PreOddsDatabaseOptions` defaults to PostgreSQL and no longer enables sensitive EF logging unless explicitly configured.
6. SportMonks sync tracking can read PostgreSQL connection string from `PREODDS_POSTGRES_CONNECTION` or `ConnectionStrings:PreOddsApiPostgresDb`.
7. Core/Odds worker projects no longer reference old MySQL worker insert infrastructure.
