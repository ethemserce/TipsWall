-- SportMonks v3 has no public /v3/core/sports listing endpoint; sport metadata
-- is only returned via include=sport on other entities. Until the football
-- writer extracts and upserts sports from league/fixture responses, seed the
-- single sport row used by the football pipeline so foreign keys on
-- competition.leagues, football.fixtures, football.teams etc. resolve.
--
-- Idempotent: ON CONFLICT (id) DO UPDATE keeps the row in sync if SportMonks
-- ever changes the canonical name/code.

insert into catalog.sports (id, name, code, last_synced_at)
values (1, 'Football', 'football', now())
on conflict (id) do update set
    name = excluded.name,
    code = excluded.code,
    last_synced_at = now(),
    updated_at = now();
