-- Admin role gate for the upcoming admin dashboard (web/).
--
-- The mobile app keeps using the existing tier ladder (guest / free /
-- premium) for product gating. `is_admin` is orthogonal — a premium
-- user is NOT automatically an admin; ops staff get the flag flipped
-- manually via:
--
--   UPDATE app.users SET is_admin = TRUE WHERE email = '...';
--
-- The flag is stamped into the JWT as the `admin` claim at token issue
-- time (see AuthController.GenerateAccessToken). [Authorize(Policy =
-- "AdminOnly")] on the new /api/v3/admin/* routes checks for that claim.
--
-- Default FALSE keeps every existing user as a non-admin; rotation
-- happens by the next refresh-token swap (~15 min) once we flip a row.

alter table app.users
    add column if not exists is_admin boolean not null default false;

create index if not exists idx_app_users_is_admin
    on app.users (is_admin) where is_admin = true;
