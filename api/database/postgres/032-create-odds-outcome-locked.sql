-- Migration 032: odds.evaluate_outcome_locked
--
-- Companion to odds.evaluate_outcome from migration 028. The original
-- function answers "given final scores, what's the verdict?" — which
-- is correct for finished matches but conservative for in-play. Many
-- markets actually lock in mid-match the moment the running score
-- crosses a threshold:
--
--   * GOALS_OVER_UNDER 2.5 with current total 4 → Over wins, locked
--     (goals can't be subtracted; only annulled, which is rare).
--   * BTTS Yes with both teams already on the scoresheet → Yes wins,
--     locked (a goal already scored can't un-happen).
--   * CLEAN_SHEET_HOME once the home team conceded → No is locked.
--
-- This function returns a verdict only when it's monotonically locked
-- from the running score. For markets that can flip in either
-- direction mid-match (1X2, correct score, etc.) it returns null —
-- the caller falls back to the upstream evaluate_outcome only after
-- state transitions to a terminal one.
--
-- HT-decidable markets are dispatched to evaluate_outcome unchanged:
-- once ht_h/ht_a are recorded (i.e. the 1st half ended), those
-- markets are settled by definition. This is also what makes the
-- "İY X" prediction flip red during a 2nd half when HT was 1-2.

create or replace function odds.evaluate_outcome_locked(
    p_developer_name text,
    p_label text,
    p_total text,
    p_handicap text,
    p_cur_h int,         -- current home goals (live or final)
    p_cur_a int,         -- current away goals (live or final)
    p_ht_h int,
    p_ht_a int,
    p_home_name text,
    p_away_name text
) returns boolean
language plpgsql
immutable
as $$
declare
    v_label text := lower(trim(coalesce(p_label, '')));
    v_total numeric := odds._parse_handicap_or_total(p_total);
    v_sum int;
    v_can_decide boolean;
begin
    if p_cur_h is null or p_cur_a is null then return null; end if;
    v_sum := p_cur_h + p_cur_a;

    case p_developer_name
    -- ─── HT-decidable markets ─────────────────────────────────────
    -- Defer to evaluate_outcome with HT scores. These markets settle
    -- the instant ht_h/ht_a are recorded — which happens as the
    -- referee blows the whistle for half time.
    when 'HALF_TIME_RESULT',
         'HALF_TIME_FULL_TIME', 'HT_FT_DOUBLE',
         'HALF_TIME_CORRECT_SCORE', 'CORRECT_SCORE_1ST_HALF',
         '1ST_HALF_GOALS',
         'FIRST_HALF_EXACT_GOALS',
         'ODD_EVEN_1ST_HALF', '1ST_HALF_GOALS_ODD_EVEN',
         'TO_WIN_1ST_HALF',
         'BOTH_TEAMS_TO_SCORE_IN_1ST_HALF',
         'TEAM_TO_SCORE_IN_1ST_HALF',
         '1ST_HALF_ASIAN_HANDICAP', 'ALTERNATIVE_1ST_HALF_ASIAN_HANDICAP',
         '1ST_HALF_HANDICAP', 'ALTERNATIVE_1ST_HALF_HANDICAP_RESULT',
         '3_WAY_HANDICAP_1ST_HALF',
         'DRAW_NO_BET_1ST_HALF',
         'DOUBLE_CHANGE_1ST_HALF'
    then
        return odds.evaluate_outcome(
            p_developer_name, p_label, p_total, p_handicap,
            null, null,  -- FT scores intentionally null; HT logic doesn't need them
            p_ht_h, p_ht_a, p_home_name, p_away_name);

    -- ─── Over/Under totals (monotonic up) ─────────────────────────
    -- Once current_total > line, "Over" is locked won and "Under"
    -- is locked lost. The reverse (total < line) is NOT locked
    -- because more goals could push it over. We only return a
    -- definitive answer for the crossed direction.
    when 'MATCH_GOALS', 'GOALS_OVER_UNDER', 'ALTERNATIVE_TOTAL_GOALS',
         'GOAL_LINE', 'ALTERNATIVE_MATCH_GOALS', 'ALTERNATIVE_GOAL_LINE'
    then
        if v_total is null then return null; end if;
        v_can_decide := v_sum::numeric > v_total;
        if not v_can_decide then return null; end if;
        if v_label in ('over', 'o') then return true; end if;
        if v_label in ('under', 'u') then return false; end if;
        return null;

    -- ─── Both Teams To Score (monotonic when both scored) ─────────
    -- The Yes side locks won the instant both teams have at least
    -- one goal. The No side locks lost in the same moment. The
    -- mirror (one team still at 0) is NOT locked — they could
    -- score later.
    when 'BOTH_TEAMS_TO_SCORE' then
        if p_cur_h >= 1 and p_cur_a >= 1 then
            if v_label in ('yes', 'y') then return true; end if;
            if v_label in ('no',  'n') then return false; end if;
        end if;
        return null;

    -- ─── Side-specific "team to score" (monotonic) ─────────────────
    when 'HOME_TEAM_TO_SCORE' then
        if p_cur_h >= 1 then
            if v_label in ('yes', 'y') then return true; end if;
            if v_label in ('no',  'n') then return false; end if;
        end if;
        return null;

    when 'AWAY_TEAM_TO_SCORE' then
        if p_cur_a >= 1 then
            if v_label in ('yes', 'y') then return true; end if;
            if v_label in ('no',  'n') then return false; end if;
        end if;
        return null;

    -- ─── Clean sheet (monotonic when conceded) ────────────────────
    -- "Yes" locks LOST once the team concedes; "No" locks WON in
    -- the same moment. The mirror (still 0 conceded) waits for FT.
    when 'CLEAN_SHEET_HOME' then
        if p_cur_h >= 1 then
            -- SportMonks semantic: "clean sheet home / yes" = "home
            -- team failed to score" (see comment in evaluate_outcome).
            -- So once home scores, "yes" is locked false and "no"
            -- is locked true.
            if v_label in ('yes', 'y') then return false; end if;
            if v_label in ('no',  'n') then return true;  end if;
        end if;
        return null;

    when 'CLEAN_SHEET_AWAY' then
        if p_cur_a >= 1 then
            if v_label in ('yes', 'y') then return false; end if;
            if v_label in ('no',  'n') then return true;  end if;
        end if;
        return null;

    -- ─── Win to nil (monotonic loss once opposite scored) ─────────
    when 'WIN_TO_NIL_HOME' then
        if p_cur_a >= 1 then
            if v_label in ('yes', 'y') then return false; end if;
            if v_label in ('no',  'n') then return true;  end if;
        end if;
        return null;

    when 'WIN_TO_NIL_AWAY' then
        if p_cur_h >= 1 then
            if v_label in ('yes', 'y') then return false; end if;
            if v_label in ('no',  'n') then return true;  end if;
        end if;
        return null;

    -- ─── HOME/AWAY team exact goals (monotonic when exceeded) ─────
    -- "Exactly N" goes to false the moment the team scores N+1.
    -- "N or more" goes to true the moment they reach N.
    when 'HOME_TEAM_EXACT_GOALS' then
        return odds._eval_team_exact_locked(v_label, p_cur_h);

    when 'AWAY_TEAM_EXACT_GOALS' then
        return odds._eval_team_exact_locked(v_label, p_cur_a);

    -- ─── HOME/AWAY team goals over/under (monotonic) ──────────────
    when 'HOME_TEAM_GOALS' then
        if v_total is null then return null; end if;
        if p_cur_h::numeric > v_total then
            if v_label in ('over', 'o') then return true; end if;
            if v_label in ('under', 'u') then return false; end if;
        end if;
        return null;

    when 'AWAY_TEAM_GOALS' then
        if v_total is null then return null; end if;
        if p_cur_a::numeric > v_total then
            if v_label in ('over', 'o') then return true; end if;
            if v_label in ('under', 'u') then return false; end if;
        end if;
        return null;

    -- ─── EXACT_TOTAL_GOALS (monotonic when exceeded) ──────────────
    when 'EXACT_TOTAL_GOALS' then
        -- Labels: "2", "3+", "2 goals", "3+ goals", "more 2"
        return odds._eval_total_exact_locked(v_label, v_sum);

    -- ─── NUMBER_OF_GOALS_IN_MATCH ─────────────────────────────────
    when 'NUMBER_OF_GOALS_IN_MATCH' then
        -- "over N goals" locks once v_sum > N
        if v_label like 'over %' then
            declare v_n int := nullif(regexp_replace(v_label, '[^0-9]', '', 'g'), '')::int;
            begin
                if v_n is null then return null; end if;
                if v_sum > v_n then return true; end if;
                return null;
            end;
        end if;
        if v_label like 'under %' then
            declare v_n int := nullif(regexp_replace(v_label, '[^0-9]', '', 'g'), '')::int;
            begin
                if v_n is null then return null; end if;
                if v_sum >= v_n then return false; end if;
                return null;
            end;
        end if;
        return null;

    -- ─── TOTAL_GOALS_3_WAY (over locks like O/U) ──────────────────
    when 'TOTAL_GOALS_3_WAY' then
        declare v_n int := nullif(regexp_replace(v_label, '[^0-9]', '', 'g'), '')::int;
        begin
            if v_n is null then return null; end if;
            if v_label like 'over %' and v_sum > v_n then return true; end if;
            if v_label like 'under %' and v_sum >= v_n then return false; end if;
            if v_label like 'exactly %' and v_sum > v_n then return false; end if;
            return null;
        end;

    -- All other markets (FT 1X2, double chance, correct score, etc.)
    -- can flip until full time. Caller falls back to evaluate_outcome
    -- only once state transitions to a terminal value.
    else
        return null;
    end case;
end;
$$;

-- Helper: HOME/AWAY exact goals — labels like "1", "2", "3+", "1 Goal",
-- "Aarhus - 2 Goals", "Aarhus - 3+ Goals", or "more 2".
create or replace function odds._eval_team_exact_locked(
    p_label text, p_team_goals int
) returns boolean
language plpgsql
immutable
as $$
declare
    v_label text := lower(trim(coalesce(p_label, '')));
    v_target int;
    v_plus boolean;
    v_tail text;
begin
    if p_team_goals is null then return null; end if;
    -- "Aarhus - 3+ Goals" → tail "3+ goals"
    if position(' - ' in v_label) > 0 then
        v_tail := trim(split_part(v_label, ' - ', 2));
    else
        v_tail := v_label;
    end if;
    -- "more 2" / "more than 2"
    if v_tail like 'more %' then
        v_target := nullif(regexp_replace(v_tail, '[^0-9]', '', 'g'), '')::int;
        if v_target is null then return null; end if;
        if p_team_goals >= v_target then return true; end if;
        return null;
    end if;
    v_target := nullif(regexp_replace(v_tail, '[^0-9]', '', 'g'), '')::int;
    if v_target is null then return null; end if;
    v_plus := v_tail like '%+%';
    if v_plus then
        -- "3+ goals" — locked true once team_goals >= 3
        if p_team_goals >= v_target then return true; end if;
        return null;
    else
        -- "exactly N" — locked false once team_goals > N
        if p_team_goals > v_target then return false; end if;
        return null;
    end if;
end;
$$;

-- Helper: EXACT_TOTAL_GOALS — labels like "2", "3+", "2 goals", "3+ goals"
create or replace function odds._eval_total_exact_locked(
    p_label text, p_sum int
) returns boolean
language plpgsql
immutable
as $$
declare
    v_label text := lower(trim(coalesce(p_label, '')));
    v_target int;
    v_plus boolean;
begin
    if p_sum is null then return null; end if;
    if v_label like 'more %' then
        v_target := nullif(regexp_replace(v_label, '[^0-9]', '', 'g'), '')::int;
        if v_target is null then return null; end if;
        if p_sum >= v_target then return true; end if;
        return null;
    end if;
    v_target := nullif(regexp_replace(v_label, '[^0-9]', '', 'g'), '')::int;
    if v_target is null then return null; end if;
    v_plus := v_label like '%+%';
    if v_plus then
        if p_sum >= v_target then return true; end if;
        return null;
    else
        if p_sum > v_target then return false; end if;
        return null;
    end if;
end;
$$;

comment on function odds.evaluate_outcome_locked(text, text, text, text, int, int, int, int, text, text) is
    'Returns a verdict only when it is monotonically locked from the running score (Over crossed,
     BTTS both scored, etc.). For HT-decidable markets defers to evaluate_outcome with HT scores.
     Caller passes running scores during live play and switches to evaluate_outcome once the
     match transitions to a terminal state.';
