# Task 023 - PostgreSQL Player, Coach, Squad, and Rival Sync Writer

## Goal

Add PostgreSQL sync support for SportMonks football player, coach, team squad, and team rival reference data.

## SportMonks Endpoints

Official SportMonks API v3 endpoint references used for this task:

- `players`: https://docs.sportmonks.com/v3/endpoints-and-entities/endpoints/players/get-all-players
- `coaches`: https://docs.sportmonks.com/v3/endpoints-and-entities/endpoints/coaches/get-all-coaches
- `rivals`: https://docs.sportmonks.com/v3/endpoints-and-entities/endpoints/rivals/get-all-rivals
- `squads/teams/{ID}`: https://docs.sportmonks.com/v3/endpoints-and-entities/endpoints/team-squads/get-team-squad-by-team-id

The team squad endpoint is team-scoped and not paginated, so it is guarded by configuration.

## Scope

- Add `ISportMonksPlayerCoachSquadRivalWriter`.
- Add `SportMonksPlayerCoachSquadRivalWriter`.
- Register the writer in SportMonks DI.
- Upsert player rows into `football.players`.
- Upsert coach rows into `football.coaches`.
- Upsert team rival rows into `football.team_rivals`.
- Upsert team squad rows into `football.team_squads`.
- Extend the football worker with an opt-in `SportMonksPlayerReferenceSync` block.
- Make player, coach, and team squad DTOs more tolerant of nullable SportMonks fields.

## Runtime Controls

`SportMonksPlayerReferenceSync` defaults to disabled.

```json
{
  "SportMonksPlayerReferenceSync": {
    "Enabled": false,
    "SyncPlayers": true,
    "SyncCoaches": true,
    "SyncRivals": true,
    "SyncTeamSquads": false,
    "MaxSquadTeamsPerRun": 0
  }
}
```

- `Enabled`: enables the whole reference sync block.
- `SyncPlayers`: calls `players`.
- `SyncCoaches`: calls `coaches`.
- `SyncRivals`: calls `rivals`.
- `SyncTeamSquads`: calls `squads/teams/{ID}` for the teams already fetched by the team sync.
- `MaxSquadTeamsPerRun`: limits team squad calls per worker loop. `0` means no explicit limit when squad sync is enabled.

## Safety Notes

- FK-sensitive writes use existing reference rows when available.
- Squad and rival rows are skipped when required team/player FK rows are not present.
- `team_squads.transfer_id` is set only when the transfer row already exists, because transfer import is a later task.
- No live SportMonks import is run in this task.

## Out of Scope

- Standings and top scorers.
- Transfers.
- Sidelined players.
- Player/coach statistics.
- Historical squad backfill by season.
- Worker scheduling and cursor-based batch planning.
- Web/mobile read API changes.

## Acceptance Tests

1. `dotnet build PreOddsApi.sln --no-restore` completes with zero errors.
2. `ISportMonksPlayerCoachSquadRivalWriter` is registered in SportMonks DI.
3. Football worker can call `players`, `coaches`, and `rivals` when `SportMonksPlayerReferenceSync:Enabled=true`.
4. Team squad sync is opt-in and uses `squads/teams/{ID}` only when `SportMonksPlayerReferenceSync:SyncTeamSquads=true`.
5. Player, coach, squad, and rival writes are idempotent through PostgreSQL `on conflict` upserts.
6. The repository does not contain a real SportMonks token.
