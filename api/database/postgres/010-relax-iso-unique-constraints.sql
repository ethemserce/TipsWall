-- SportMonks returns sub-national entities and territories sharing ISO codes
-- with their parent country (e.g. Wales/England/Scotland all carry iso2='GB';
-- US territories share 'US'). The original unique partial indexes on iso2/iso3
-- caused 23505 unique-constraint violations during catalog reference sync,
-- aborting the entire countries+regions+cities transaction.
--
-- Drop the unique partial indexes and replace them with non-unique partial
-- lookup indexes so iso lookups stay fast without rejecting duplicates.

drop index if exists catalog.ux_countries_iso2;
drop index if exists catalog.ux_countries_iso3;

create index if not exists ix_countries_iso2
    on catalog.countries (iso2)
    where iso2 is not null;

create index if not exists ix_countries_iso3
    on catalog.countries (iso3)
    where iso3 is not null;
