# Task 11 - SportMonks API Client Infrastructure

Branch: `task/011-sportmonks-api-client-infrastructure`

## Scope

This task adds the first SportMonks v3 client infrastructure layer for the new PreOdds backend.

It covers a typed HTTP client, request/options models, secure token handling, pagination, timeout, retry, and logging foundations. It does not migrate worker business flows, write API payloads to PostgreSQL, create sync jobs, or call SportMonks during acceptance testing.

## Inputs Reviewed

- Existing static `SportMonksApi` RestSharp wrapper.
- Existing SportMonks response wrapper models: `SportMonksBase<T>` and `Pagination`.
- Existing worker usage for core, football fixture, and odds imports.
- SportMonks API v3 documentation for authentication, pagination, includes, filters, and rate limits.

## Design Decisions

- Added `ISportMonksApiClient` as the new injectable contract.
- Added `SportMonksApiClient` as the typed `HttpClient` implementation.
- Kept the old static `SportMonksApi` facade as a compatibility adapter so existing workers continue to compile while future tasks migrate them to DI.
- Token lookup supports the modern `SportMonks:ApiToken` key and the legacy `SportMonksValues:api_key` key.
- Environment variables override file configuration. The preferred variable is `PREODDS_SPORTMONKS_TOKEN`.
- Placeholder values such as `CHANGE_ME_SPORTMONKS_TOKEN` are treated as missing tokens.
- Header authentication is the default so frontend clients never need the SportMonks token.
- When header authentication is active, secret query keys such as `api_token`, `api_key`, and `token` are removed before sending/logging requests.
- Pagination follows `pagination.has_more` and `pagination.next_page`.
- Default `per_page` is 50, matching SportMonks v3 normal page limits.
- Retry is implemented for HTTP 429 and 5xx responses, respecting `Retry-After` when present.
- Request logging records endpoint path, status code, and elapsed time without exposing tokens.
- Web API DI registration now registers the SportMonks client through `DependencyService`.

## Files Added

- `PreOddsApi.ExternalApis/SportMonks/ISportMonksApiClient.cs`
- `PreOddsApi.ExternalApis/SportMonks/SportMonksApiClient.cs`
- `PreOddsApi.ExternalApis/SportMonks/SportMonksApiOptions.cs`
- `PreOddsApi.ExternalApis/SportMonks/SportMonksApiRequest.cs`
- `PreOddsApi.ExternalApis/SportMonks/SportMonksApiException.cs`
- `PreOddsApi.ExternalApis/DependencyInjection/SportMonksApiServiceCollectionExtensions.cs`

## Files Updated

- `PreOddsApi.ExternalApis/SportMonks/SportMonksApi.cs`
- `PreOddsApi.ExternalApis/PreOddsApi.ExternalApis.csproj`
- `PreOddsApi.BusinessLayer/PreOddsApi.BusinessLayer.csproj`
- `PreOddsApi.BusinessLayer/DependencyInjection/DependencyService.cs`
- `PreOddsApi.WebApi/appsettings.example.json`
- `PreOddsApi.Worker/SportMonks/SportMonks.Core/appsettings.example.json`
- `PreOddsApi.Worker/SportMonks/SportMonks.Football.Fixture/appsettings.example.json`
- `PreOddsApi.Worker/SportMonks/SportMonks.Odds/appsettings.example.json`

## Example Usage

```csharp
var request = SportMonksApiRequest.Create("fixtures/date/2026-05-03")
    .WithInclude("participants;events;statistics")
    .WithQueryParameter("timezone", "Europe/Istanbul");

var fixtures = await sportMonksApiClient.GetAllAsync<Fixture>(request, cancellationToken);
```

## Acceptance Criteria

Task 11 is accepted when:

1. `ISportMonksApiClient` can be injected through DI.
2. The client supports SportMonks v3 GET calls with query parameters, includes, filters, timezone, and default pagination.
3. The client follows `pagination.has_more` and `pagination.next_page` for all-page fetches.
4. The token can be supplied by `PREODDS_SPORTMONKS_TOKEN` without being committed to source control.
5. Token values are not written into application logs.
6. Existing worker code still compiles through the compatibility `SportMonksApi` facade.
7. Existing solution build succeeds.

## Verification

```text
dotnet restore PreOddsApi.sln
dotnet build PreOddsApi.sln --no-restore
```

Build result: succeeded with existing warnings.
