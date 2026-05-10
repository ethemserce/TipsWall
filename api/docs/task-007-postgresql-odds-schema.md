# Task 7 - PostgreSQL Odds Schema

Branch: `task/007-postgresql-odds-schema`

## Scope

This task adds the PostgreSQL odds schema for SportMonks v3.

It covers bookmaker and market reference data, pre-match odds, in-play odds, odds movement history tables for future syncs, and bookmaker fixture event mappings. It does not add analytics, app-owned tables, sync workers, API clients, or legacy data migration.

## Inputs Reviewed

- Existing legacy entities: `bookmaker`, `market`, `odd`
- Existing SportMonks v3 models: `Bookmaker`, `Market`, `PreMatchOdd`, `InplayOdd`
- SportMonks v3 docs:
  - https://docs.sportmonks.com/v3/endpoints-and-entities/endpoints/standard-odds-feed/pre-match-odds
  - https://docs.sportmonks.com/v3/endpoints-and-entities/endpoints/standard-odds-feed/inplay-odds
  - https://docs.sportmonks.com/v3/endpoints-and-entities/endpoints/bookmakers
  - https://docs.sportmonks.com/v3/endpoints-and-entities/endpoints/markets
  - https://docs.sportmonks.com/v3/endpoints-and-entities/endpoints/premium-odds-feed/premium-pre-match-odds/get-all-premium-odds
  - https://docs.sportmonks.com/v3/endpoints-and-entities/endpoints/premium-odds-feed/premium-pre-match-odds/get-all-historical-odds

## Tables Added

- `odds.bookmakers`
- `odds.markets`
- `odds.prematch_odds_current`
- `odds.prematch_odds_history`
- `odds.inplay_odds_current`
- `odds.inplay_odds_history`

## Design Decisions

- SportMonks v3 IDs are used as primary keys for provider-owned current/reference records.
- `legacy_id` is preserved on bookmakers and markets because SportMonks v3 IDs can differ from older feeds.
- Standard and premium odds share the same current/history table shape through `feed_type`.
- `available_in_standard` and `available_in_premium` flags allow one bookmaker or market record to be enriched from multiple SportMonks endpoints.
- Current odds tables are optimized for web/mobile read paths by fixture, bookmaker, market, and `outcome_key`.
- History tables are for future SportMonks sync snapshots and premium historical odds. They are not a legacy data migration path.
- Premium historical odds can arrive with only `odd_id` and price fields, so `prematch_odds_history` keeps fixture, bookmaker, market, and outcome fields nullable for later enrichment.
- Odds values and probabilities are stored in parsed numeric fields, while text fields such as `probability_text` and `american_text` preserve original API formatting.

## Script

SQL file:

```text
database/postgres/006-create-odds-schema.sql
```

## Acceptance Criteria

Task 7 is accepted when:

1. Bookmaker and market PostgreSQL tables are defined with v3 IDs and optional `legacy_id`.
2. Pre-match current/history odds tables are defined for standard and premium feeds.
3. In-play current/history odds tables are defined.
4. Bookmaker fixture mapping data has a place in the schema.
5. Script can be applied after baseline, catalog, competition, football core, and football detail scripts.
6. Script does not create analytics, app-owned tables, sync workers, API clients, or legacy migration logic.
7. Existing application build still succeeds.
