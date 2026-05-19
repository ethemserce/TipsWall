-- Migration 034: 3-way handicap markets must not push when handicap-
-- adjusted score is level.
--
-- Bug: odds._eval_handicap returned NULL for v_diff = 0 regardless of
-- the calling market. That is correct for Asian Handicap (whole-line
-- push = refund), but WRONG for 3-way handicap markets
-- (HANDICAP_RESULT / 3_WAY_HANDICAP / ALTERNATIVE_HANDICAP_RESULT and
-- their 1st/2nd-half variants), where the handicap-adjusted level is
-- the "X" outcome — labels "1" and "2" must resolve to FALSE (loss),
-- not NULL.
--
-- Symptom reported 2026-05-19: Ried 2-1 Wolfsberger (FT), bet on
-- "DEP +1" (HANDICAP_RESULT, market_id 56) rendered with no win/loss
-- colour because evaluate_outcome returned NULL. Correct value is
-- FALSE (handicap-adjusted 2-2 → X wins → bet on 2 lost).
--
-- Fix: extend _eval_handicap with a p_three_way flag. When true, the
-- "level after handicap" branch returns FALSE for 1/2 labels (instead
-- of NULL). The "X" label branch is unchanged (it already returns
-- TRUE on level, regardless of mode). The dispatcher in
-- evaluate_outcome is updated to pass true for the 3-way market
-- families and false for Asian Handicap families.
--
-- Affected stored rows: any prematch_odds_current row whose market is
-- one of the 3-way handicap families AND whose handicap-adjusted final
-- score equals zero on the 1/2 side. The mobile /signals endpoint
-- already re-evaluates winning at read time via evaluate_outcome
-- (see migration 028 + 032), so those rows surface the corrected value
-- on the next fetch without a backfill. Stored .winning columns can be
-- left stale or re-graded via the same one-shot CTE used previously
-- (commit b91c4a5-era re-grade script); not required for UI correctness
-- since read-time re-compute already covers it.

create or replace function odds._eval_handicap(
    p_label text, p_handicap text,
    p_home int, p_away int,
    p_home_name text, p_away_name text,
    p_three_way boolean default false
) returns boolean
language plpgsql
immutable
as $$
declare
    v_label text := lower(trim(coalesce(p_label, '')));
    v_hcap numeric := odds._parse_handicap_or_total(p_handicap);
    v_diff numeric;
    v_h_name text := lower(coalesce(p_home_name, ''));
    v_a_name text := lower(coalesce(p_away_name, ''));
begin
    if v_hcap is null or p_home is null or p_away is null then return null; end if;
    if v_label in ('1', 'home') or (v_h_name <> '' and v_label = v_h_name) then
        v_diff := (p_home::numeric + v_hcap) - p_away::numeric;
    elsif v_label in ('2', 'away') or (v_a_name <> '' and v_label = v_a_name) then
        v_diff := (p_away::numeric + v_hcap) - p_home::numeric;
    elsif v_label in ('x', 'draw') then
        -- "X" outcome only exists on 3-way handicap markets; on Asian
        -- markets there's no X line so this branch is unreachable from
        -- the dispatcher, but we leave it consistent for safety.
        return ((p_home::numeric + v_hcap) = p_away::numeric);
    else
        return null;
    end if;
    return case
        when v_diff > 0 then true
        when v_diff < 0 then false
        -- v_diff = 0: handicap-adjusted score is level.
        --   * Asian Handicap (whole line) → push, refund → NULL
        --   * 3-way handicap → X wins, so 1/2 both LOST → FALSE
        else case when p_three_way then false else null end
    end;
end;
$$;

-- Dispatcher: 3-way families pass p_three_way = true.
create or replace function odds.evaluate_outcome(
    p_developer_name text,
    p_label text,
    p_total text,
    p_handicap text,
    p_ft_h int,
    p_ft_a int,
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
    v_h_name text := lower(coalesce(p_home_name, ''));
    v_a_name text := lower(coalesce(p_away_name, ''));
    v_sum int;
    v_ht_sum int;
    v_2h_h int;
    v_2h_a int;
    v_2h_sum int;
    v_exact int;
    v_left text;
    v_right text;
    v_n int;
begin
    if p_ft_h is null or p_ft_a is null then return null; end if;
    v_sum := p_ft_h + p_ft_a;
    if p_ht_h is not null and p_ht_a is not null then
        v_ht_sum := p_ht_h + p_ht_a;
        v_2h_h := p_ft_h - p_ht_h;
        v_2h_a := p_ft_a - p_ht_a;
        v_2h_sum := v_2h_h + v_2h_a;
    end if;

    case p_developer_name
    when 'FULLTIME_RESULT' then
        if v_label in ('1', 'home') then return p_ft_h > p_ft_a; end if;
        if v_label in ('2', 'away') then return p_ft_h < p_ft_a; end if;
        if v_label in ('x', 'draw') then return p_ft_h = p_ft_a; end if;
        return null;

    when 'DOUBLE_CHANCE' then
        if v_label in ('1x','home/draw','home or draw','1/x') then return p_ft_h >= p_ft_a; end if;
        if v_label in ('x2','draw/away','draw or away','x/2') then return p_ft_h <= p_ft_a; end if;
        if v_label in ('12','home/away','home or away','1/2') then return p_ft_h <> p_ft_a; end if;
        if position(' or ' in v_label) > 0 then
            v_left := trim(substring(v_label from 1 for position(' or ' in v_label) - 1));
            v_right := trim(substring(v_label from position(' or ' in v_label) + 4));
            if v_left = 'draw' and v_right = v_h_name then return p_ft_h >= p_ft_a; end if;
            if v_left = 'draw' and v_right = v_a_name then return p_ft_h <= p_ft_a; end if;
            if v_right = 'draw' and v_left = v_h_name then return p_ft_h >= p_ft_a; end if;
            if v_right = 'draw' and v_left = v_a_name then return p_ft_h <= p_ft_a; end if;
            if (v_left = v_h_name and v_right = v_a_name)
                or (v_left = v_a_name and v_right = v_h_name) then
                return p_ft_h <> p_ft_a;
            end if;
        end if;
        return null;

    when 'DRAW_NO_BET' then
        if p_ft_h = p_ft_a then return null; end if;
        if v_label in ('1','home') then return p_ft_h > p_ft_a; end if;
        if v_label in ('2','away') then return p_ft_h < p_ft_a; end if;
        return null;

    when 'BOTH_TEAMS_TO_SCORE' then
        if v_label in ('yes','y') then return (p_ft_h >= 1 and p_ft_a >= 1); end if;
        if v_label in ('no','n') then return (p_ft_h = 0 or p_ft_a = 0); end if;
        return null;

    when 'MATCH_GOALS','GOALS_OVER_UNDER','ALTERNATIVE_TOTAL_GOALS',
         'GOAL_LINE','ALTERNATIVE_MATCH_GOALS','ALTERNATIVE_GOAL_LINE' then
        return odds._eval_over_under(v_label, p_total, v_sum);

    when 'CORRECT_SCORE','FINAL_SCORE' then
        return v_label = (p_ft_h::text || ':' || p_ft_a::text)
            or v_label = (p_ft_h::text || '-' || p_ft_a::text);

    when 'HALF_TIME_RESULT' then
        if p_ht_h is null then return null; end if;
        if v_label in ('1','home') then return p_ht_h > p_ht_a; end if;
        if v_label in ('2','away') then return p_ht_h < p_ht_a; end if;
        if v_label in ('x','draw') then return p_ht_h = p_ht_a; end if;
        return null;

    when 'HALF_TIME_FULL_TIME','HT_FT_DOUBLE' then
        if p_ht_h is null then return null; end if;
        if position(' - ' in v_label) = 0 then return null; end if;
        v_left  := trim(substring(v_label from 1 for position(' - ' in v_label) - 1));
        v_right := trim(substring(v_label from position(' - ' in v_label) + 3));
        return (
            (v_left = 'draw' and p_ht_h = p_ht_a)
            or (v_left = v_h_name and p_ht_h > p_ht_a)
            or (v_left = v_a_name and p_ht_h < p_ht_a)
        ) and (
            (v_right = 'draw' and p_ft_h = p_ft_a)
            or (v_right = v_h_name and p_ft_h > p_ft_a)
            or (v_right = v_a_name and p_ft_h < p_ft_a)
        );

    when 'HALF_TIME_CORRECT_SCORE','CORRECT_SCORE_1ST_HALF' then
        if p_ht_h is null then return null; end if;
        return v_label = (p_ht_h::text || ':' || p_ht_a::text)
            or v_label = (p_ht_h::text || '-' || p_ht_a::text);

    when 'CORRECT_SCORE_2ND_HALF' then
        if p_ht_h is null then return null; end if;
        return v_label = (v_2h_h::text || ':' || v_2h_a::text)
            or v_label = (v_2h_h::text || '-' || v_2h_a::text);

    when '1ST_HALF_GOALS' then
        if p_ht_h is null then return null; end if;
        return odds._eval_over_under(v_label, p_total, v_ht_sum);

    when '2ND_HALF_GOALS' then
        if p_ht_h is null then return null; end if;
        return odds._eval_over_under(v_label, p_total, v_2h_sum);

    when 'FIRST_HALF_EXACT_GOALS' then
        if p_ht_h is null then return null; end if;
        begin v_exact := nullif(regexp_replace(v_label, '[^0-9]', '', 'g'), '')::int;
        exception when others then return null; end;
        if v_exact is null then return null; end if;
        if v_label like '%+%' then return v_ht_sum >= v_exact; end if;
        return v_ht_sum = v_exact;

    when 'SECOND_HALF_EXACT_GOALS' then
        if p_ht_h is null then return null; end if;
        begin v_exact := nullif(regexp_replace(v_label, '[^0-9]', '', 'g'), '')::int;
        exception when others then return null; end;
        if v_exact is null then return null; end if;
        if v_label like '%+%' then return v_2h_sum >= v_exact; end if;
        return v_2h_sum = v_exact;

    when 'ODD_EVEN' then
        if v_label = 'odd' then return (v_sum % 2) = 1; end if;
        if v_label = 'even' then return (v_sum % 2) = 0; end if;
        return null;

    when 'HOME_ODD_EVEN' then
        if v_label = 'odd' then return (p_ft_h % 2) = 1; end if;
        if v_label = 'even' then return (p_ft_h % 2) = 0; end if;
        return null;

    when 'AWAY_ODD_EVEN' then
        if v_label = 'odd' then return (p_ft_a % 2) = 1; end if;
        if v_label = 'even' then return (p_ft_a % 2) = 0; end if;
        return null;

    when 'ODD_EVEN_1ST_HALF','1ST_HALF_GOALS_ODD_EVEN' then
        if p_ht_h is null then return null; end if;
        if v_label = 'odd' then return (v_ht_sum % 2) = 1; end if;
        if v_label = 'even' then return (v_ht_sum % 2) = 0; end if;
        return null;

    when 'ODD_EVEN_2ND_HALF','2ND_HALF_GOALS_ODD_EVEN' then
        if p_ht_h is null then return null; end if;
        if v_label = 'odd' then return (v_2h_sum % 2) = 1; end if;
        if v_label = 'even' then return (v_2h_sum % 2) = 0; end if;
        return null;

    when 'HOME_TEAM_GOALS' then
        return odds._eval_over_under(v_label, p_total, p_ft_h);

    when 'AWAY_TEAM_GOALS' then
        return odds._eval_over_under(v_label, p_total, p_ft_a);

    when 'TEAM_TOTAL_GOALS' then
        if v_label like 'home %' or v_label like '1 %' then
            return odds._eval_over_under(split_part(v_label, ' ', 2), p_total, p_ft_h);
        end if;
        if v_label like 'away %' or v_label like '2 %' then
            return odds._eval_over_under(split_part(v_label, ' ', 2), p_total, p_ft_a);
        end if;
        return null;

    when 'HOME_TEAM_EXACT_GOALS' then
        if v_label like 'more %' then
            begin v_exact := nullif(regexp_replace(v_label, '[^0-9]', '', 'g'), '')::int;
            exception when others then return null; end;
            if v_exact is null then return null; end if;
            return p_ft_h >= v_exact;
        end if;
        if v_label ~ '^\d+\+?$' then
            v_n := substring(v_label from '\d+')::int;
            if v_label like '%+' then return p_ft_h >= v_n; end if;
            return p_ft_h = v_n;
        end if;
        if v_label like '% goal%' then
            begin v_exact := nullif(regexp_replace(v_label, '[^0-9]', '', 'g'), '')::int;
            exception when others then return null; end;
            if v_exact is null then return null; end if;
            if v_label like '%+ goal%' then return p_ft_h >= v_exact; end if;
            return p_ft_h = v_exact;
        end if;
        return null;

    when 'AWAY_TEAM_EXACT_GOALS' then
        if v_label like 'more %' then
            begin v_exact := nullif(regexp_replace(v_label, '[^0-9]', '', 'g'), '')::int;
            exception when others then return null; end;
            if v_exact is null then return null; end if;
            return p_ft_a >= v_exact;
        end if;
        if v_label ~ '^\d+\+?$' then
            v_n := substring(v_label from '\d+')::int;
            if v_label like '%+' then return p_ft_a >= v_n; end if;
            return p_ft_a = v_n;
        end if;
        if v_label like '% goal%' then
            begin v_exact := nullif(regexp_replace(v_label, '[^0-9]', '', 'g'), '')::int;
            exception when others then return null; end;
            if v_exact is null then return null; end if;
            if v_label like '%+ goal%' then return p_ft_a >= v_exact; end if;
            return p_ft_a = v_exact;
        end if;
        return null;

    when 'EXACT_TOTAL_GOALS' then
        if v_label ~ '^\d+\+?$' then
            v_n := substring(v_label from '\d+')::int;
            if v_label like '%+' then return v_sum >= v_n; end if;
            return v_sum = v_n;
        end if;
        if v_label like '% goal%' then
            begin v_exact := nullif(regexp_replace(v_label, '[^0-9]', '', 'g'), '')::int;
            exception when others then return null; end;
            if v_exact is null then return null; end if;
            if v_label like '%+ goal%' then return v_sum >= v_exact; end if;
            return v_sum = v_exact;
        end if;
        if v_label like 'more %' then
            begin v_exact := nullif(regexp_replace(v_label, '[^0-9]', '', 'g'), '')::int;
            exception when others then return null; end;
            if v_exact is null then return null; end if;
            return v_sum >= v_exact;
        end if;
        return null;

    when 'TO_WIN_1ST_HALF' then
        if p_ht_h is null then return null; end if;
        if v_label in ('1','home') then return p_ht_h > p_ht_a; end if;
        if v_label in ('2','away') then return p_ht_h < p_ht_a; end if;
        if v_label in ('x','draw') then return p_ht_h = p_ht_a; end if;
        return null;

    when 'TO_WIN_2ND_HALF','2ND_HALF_RESULT' then
        if p_ht_h is null then return null; end if;
        if v_label in ('1','home') then return v_2h_h > v_2h_a; end if;
        if v_label in ('2','away') then return v_2h_h < v_2h_a; end if;
        if v_label in ('x','draw') then return v_2h_h = v_2h_a; end if;
        return null;

    when 'HOME_TEAM_WIN_BOTH_HALVES' then
        if p_ht_h is null then return null; end if;
        if v_label in ('yes','y') then return (p_ht_h > p_ht_a) and (v_2h_h > v_2h_a); end if;
        if v_label in ('no','n') then return not ((p_ht_h > p_ht_a) and (v_2h_h > v_2h_a)); end if;
        return null;

    when 'AWAY_TEAM_WIN_BOTH_HALVES' then
        if p_ht_h is null then return null; end if;
        if v_label in ('yes','y') then return (p_ht_a > p_ht_h) and (v_2h_a > v_2h_h); end if;
        if v_label in ('no','n') then return not ((p_ht_a > p_ht_h) and (v_2h_a > v_2h_h)); end if;
        return null;

    when 'TO_WIN_BOTH_HALVES' then
        if p_ht_h is null then return null; end if;
        if v_label in ('1') then return (p_ht_h > p_ht_a) and (v_2h_h > v_2h_a); end if;
        if v_label in ('2') then return (p_ht_a > p_ht_h) and (v_2h_a > v_2h_h); end if;
        return null;

    when 'CLEAN_SHEET_HOME' then
        if v_label in ('yes','y') then return p_ft_h = 0; end if;
        if v_label in ('no','n') then return p_ft_h > 0; end if;
        return null;

    when 'CLEAN_SHEET_AWAY' then
        if v_label in ('yes','y') then return p_ft_a = 0; end if;
        if v_label in ('no','n') then return p_ft_a > 0; end if;
        return null;

    when 'WIN_TO_NIL' then
        if v_label in ('1','home') then return p_ft_h > p_ft_a and p_ft_a = 0; end if;
        if v_label in ('2','away') then return p_ft_a > p_ft_h and p_ft_h = 0; end if;
        return null;

    when 'WIN_TO_NIL_HOME' then
        if v_label in ('yes','y') then return p_ft_h > p_ft_a and p_ft_a = 0; end if;
        if v_label in ('no','n') then return not (p_ft_h > p_ft_a and p_ft_a = 0); end if;
        return null;

    when 'WIN_TO_NIL_AWAY' then
        if v_label in ('yes','y') then return p_ft_a > p_ft_h and p_ft_h = 0; end if;
        if v_label in ('no','n') then return not (p_ft_a > p_ft_h and p_ft_h = 0); end if;
        return null;

    when 'BOTH_TEAMS_TO_SCORE_IN_1ST_HALF' then
        if p_ht_h is null then return null; end if;
        if v_label in ('yes','y') then return (p_ht_h >= 1 and p_ht_a >= 1); end if;
        if v_label in ('no','n') then return (p_ht_h = 0 or p_ht_a = 0); end if;
        return null;

    when 'BOTH_TEAMS_TO_SCORE_IN_2ND_HALF' then
        if p_ht_h is null then return null; end if;
        if v_label in ('yes','y') then return (v_2h_h >= 1 and v_2h_a >= 1); end if;
        if v_label in ('no','n') then return (v_2h_h = 0 or v_2h_a = 0); end if;
        return null;

    when 'HOME_TEAM_TO_SCORE' then
        if v_label in ('yes','y') then return p_ft_h >= 1; end if;
        if v_label in ('no','n') then return p_ft_h = 0; end if;
        return null;

    when 'AWAY_TEAM_TO_SCORE' then
        if v_label in ('yes','y') then return p_ft_a >= 1; end if;
        if v_label in ('no','n') then return p_ft_a = 0; end if;
        return null;

    when 'TEAM_TO_SCORE_IN_1ST_HALF' then
        if p_ht_h is null then return null; end if;
        if v_label in ('1','home') then return p_ht_h >= 1; end if;
        if v_label in ('2','away') then return p_ht_a >= 1; end if;
        return null;

    when 'TEAM_TO_SCORE_IN_2ND_HALF' then
        if p_ht_h is null then return null; end if;
        if v_label in ('1','home') then return v_2h_h >= 1; end if;
        if v_label in ('2','away') then return v_2h_a >= 1; end if;
        return null;

    when 'TEAM_TO_SCORE_IN_BOTH_HALVES' then
        if p_ht_h is null then return null; end if;
        if v_label in ('1','home') then return p_ht_h >= 1 and v_2h_h >= 1; end if;
        if v_label in ('2','away') then return p_ht_a >= 1 and v_2h_a >= 1; end if;
        return null;

    when 'HOME_TEAM_HIGHEST_SCORING_HALF' then
        if p_ht_h is null then return null; end if;
        if v_label like '1st%' then return p_ht_h > v_2h_h; end if;
        if v_label like '2nd%' then return p_ht_h < v_2h_h; end if;
        if v_label in ('tie','draw') then return p_ht_h = v_2h_h; end if;
        return null;

    when 'AWAY_TEAM_HIGHEST_SCORING_HALF' then
        if p_ht_h is null then return null; end if;
        if v_label like '1st%' then return p_ht_a > v_2h_a; end if;
        if v_label like '2nd%' then return p_ht_a < v_2h_a; end if;
        if v_label in ('tie','draw') then return p_ht_a = v_2h_a; end if;
        return null;

    when 'HALF_WITH_MOST_GOALS' then
        if p_ht_h is null then return null; end if;
        if v_label like '1st%' then return v_ht_sum > v_2h_sum; end if;
        if v_label like '2nd%' then return v_ht_sum < v_2h_sum; end if;
        if v_label in ('tie','draw') then return v_ht_sum = v_2h_sum; end if;
        return null;

    when 'RESULT_BOTH_TEAMS_TO_SCORE' then
        if position('/' in v_label) = 0 then return null; end if;
        v_left  := trim(split_part(v_label, '/', 1));
        v_right := trim(split_part(v_label, '/', 2));
        return (
            (v_left in ('home','1') and p_ft_h > p_ft_a)
            or (v_left in ('draw','x') and p_ft_h = p_ft_a)
            or (v_left in ('away','2') and p_ft_h < p_ft_a)
        ) and (
            (v_right in ('yes','y') and p_ft_h >= 1 and p_ft_a >= 1)
            or (v_right in ('no','n') and (p_ft_h = 0 or p_ft_a = 0))
        );

    when 'RESULT_TOTAL_GOALS' then
        if position('/' in v_label) = 0 then
            return odds._eval_over_under(v_label, p_total, v_sum);
        end if;
        v_left  := trim(split_part(v_label, '/', 1));
        v_right := trim(split_part(v_label, '/', 2));
        return (
            (v_left in ('home','1') and p_ft_h > p_ft_a)
            or (v_left in ('draw','x') and p_ft_h = p_ft_a)
            or (v_left in ('away','2') and p_ft_h < p_ft_a)
        ) and (odds._eval_over_under(v_right, p_total, v_sum) is true);

    when 'TOTAL_GOALS_BOTH_TEAMS_TO_SCORE' then
        if position('/' in v_label) > 0 then
            v_left  := trim(split_part(v_label, '/', 1));
            v_right := trim(split_part(v_label, '/', 2));
            return (
                (v_left in ('o','over') and v_sum::numeric > odds._parse_handicap_or_total(p_total))
                or (v_left in ('u','under') and v_sum::numeric < odds._parse_handicap_or_total(p_total))
            ) and (
                (v_right in ('yes','y') and p_ft_h >= 1 and p_ft_a >= 1)
                or (v_right in ('no','n') and (p_ft_h = 0 or p_ft_a = 0))
            );
        end if;
        if v_label like '% & %' then
            v_left  := trim(split_part(v_label, ' & ', 1));
            v_right := trim(split_part(v_label, ' & ', 2));
            return (
                (v_left like 'over %' and v_sum::numeric > nullif(split_part(v_left,' ',2),'')::numeric)
                or (v_left like 'under %' and v_sum::numeric < nullif(split_part(v_left,' ',2),'')::numeric)
            ) and (
                (v_right in ('yes','y') and p_ft_h >= 1 and p_ft_a >= 1)
                or (v_right in ('no','n') and (p_ft_h = 0 or p_ft_a = 0))
            );
        end if;
        return null;

    when 'NUMBER_OF_GOALS_IN_MATCH' then
        if v_label like 'over %' then
            begin v_exact := nullif(regexp_replace(v_label, '[^0-9]', '', 'g'), '')::int;
            exception when others then return null; end;
            if v_exact is null then return null; end if;
            return v_sum > v_exact;
        end if;
        if v_label like 'under %' then
            begin v_exact := nullif(regexp_replace(v_label, '[^0-9]', '', 'g'), '')::int;
            exception when others then return null; end;
            if v_exact is null then return null; end if;
            return v_sum < v_exact;
        end if;
        if v_label ~ '^\d+ or \d+ goals$' then
            return v_sum = substring(v_label from '^(\d+)')::int
                or v_sum = substring(v_label from ' or (\d+) goals')::int;
        end if;
        return null;

    when 'TOTAL_GOALS_3_WAY' then
        if v_label like 'under %' then
            v_exact := nullif(regexp_replace(v_label, '[^0-9]', '', 'g'), '')::int;
            return v_sum < v_exact;
        end if;
        if v_label like 'over %' then
            v_exact := nullif(regexp_replace(v_label, '[^0-9]', '', 'g'), '')::int;
            return v_sum > v_exact;
        end if;
        if v_label like 'exactly %' then
            v_exact := nullif(regexp_replace(v_label, '[^0-9]', '', 'g'), '')::int;
            return v_sum = v_exact;
        end if;
        return null;

    -- Asian handicap families: 2-way bet, whole-line level = push (NULL).
    when 'ASIAN_HANDICAP','ALTERNATIVE_ASIAN_HANDICAP' then
        return odds._eval_handicap(p_label, p_handicap, p_ft_h, p_ft_a, p_home_name, p_away_name, false);

    -- 3-way handicap families: separate "X" outcome owns the level case,
    -- so 1/2 labels at handicap-adjusted level → LOSS (not push).
    when 'HANDICAP_RESULT','ALTERNATIVE_HANDICAP_RESULT','3_WAY_HANDICAP' then
        return odds._eval_handicap(p_label, p_handicap, p_ft_h, p_ft_a, p_home_name, p_away_name, true);

    when '1ST_HALF_ASIAN_HANDICAP','ALTERNATIVE_1ST_HALF_ASIAN_HANDICAP' then
        if p_ht_h is null then return null; end if;
        return odds._eval_handicap(p_label, p_handicap, p_ht_h, p_ht_a, p_home_name, p_away_name, false);

    when '1ST_HALF_HANDICAP','ALTERNATIVE_1ST_HALF_HANDICAP_RESULT','3_WAY_HANDICAP_1ST_HALF' then
        if p_ht_h is null then return null; end if;
        return odds._eval_handicap(p_label, p_handicap, p_ht_h, p_ht_a, p_home_name, p_away_name, true);

    when '2ND_HALF_ASIAN_HANDICAP' then
        if p_ht_h is null then return null; end if;
        return odds._eval_handicap(p_label, p_handicap, v_2h_h, v_2h_a, p_home_name, p_away_name, false);

    when '3_WAY_HANDICAP_2ND_HALF' then
        if p_ht_h is null then return null; end if;
        return odds._eval_handicap(p_label, p_handicap, v_2h_h, v_2h_a, p_home_name, p_away_name, true);

    when 'DRAW_NO_BET_1ST_HALF' then
        if p_ht_h is null then return null; end if;
        if p_ht_h = p_ht_a then return null; end if;
        if v_label in ('1','home') then return p_ht_h > p_ht_a; end if;
        if v_label in ('2','away') then return p_ht_h < p_ht_a; end if;
        return null;

    when 'DRAW_NO_BET_2ND_HALF' then
        if p_ht_h is null then return null; end if;
        if v_2h_h = v_2h_a then return null; end if;
        if v_label in ('1','home') then return v_2h_h > v_2h_a; end if;
        if v_label in ('2','away') then return v_2h_h < v_2h_a; end if;
        return null;

    when 'DOUBLE_CHANGE_1ST_HALF' then
        if p_ht_h is null then return null; end if;
        if v_label in ('1x','home/draw') then return p_ht_h >= p_ht_a; end if;
        if v_label in ('x2','draw/away') then return p_ht_h <= p_ht_a; end if;
        if v_label in ('12','home/away') then return p_ht_h <> p_ht_a; end if;
        return null;

    when 'DOUBLE_CHANGE_2ND_HALF' then
        if p_ht_h is null then return null; end if;
        if v_label in ('1x','home/draw') then return v_2h_h >= v_2h_a; end if;
        if v_label in ('x2','draw/away') then return v_2h_h <= v_2h_a; end if;
        if v_label in ('12','home/away') then return v_2h_h <> v_2h_a; end if;
        return null;

    else
        return null;
    end case;
end;
$$;

comment on function odds.evaluate_outcome(text, text, text, text, int, int, int, int, text, text) is
    'Returns whether a prematch outcome won given the (live or final) score and team names.
     Returns NULL on push, missing score, or unsupported (non-score-based) markets.
     2026-05-19: 3-way handicap markets no longer push on handicap-adjusted level —
     the X outcome owns that case, so 1/2 resolve to LOSS instead of NULL.';
