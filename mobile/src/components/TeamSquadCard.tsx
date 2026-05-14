import { Image } from 'expo-image';
import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { Pressable, StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { useTheme } from '@/src/lib/useTheme';
import type { TeamSquadMember } from '@/src/types/team';

interface TeamSquadCardProps {
  squad: TeamSquadMember[];
  onPlayerPress?: (playerId: number) => void;
}

type Bucket = {
  key: 'goalkeeper' | 'defender' | 'midfielder' | 'attacker' | 'other';
  i18nKey: string;
  defaultLabel: string;
  members: TeamSquadMember[];
};

/**
 * Roster grouped into the four football positions plus an "Other"
 * catch-all for unmapped codes. Each row is pressable so the upcoming
 * PlayerDetailScreen has a tap target ready — onPlayerPress is the
 * single integration point.
 */
export function TeamSquadCard({ squad, onPlayerPress }: TeamSquadCardProps) {
  const c = useTheme();
  const { t } = useTranslation();

  const buckets = useMemo<Bucket[]>(() => {
    const groups: Record<Bucket['key'], TeamSquadMember[]> = {
      goalkeeper: [],
      defender: [],
      midfielder: [],
      attacker: [],
      other: [],
    };
    for (const m of squad) {
      const code = (m.position_code ?? '').toUpperCase();
      const key: Bucket['key'] =
        code === 'GOALKEEPER'
          ? 'goalkeeper'
          : code === 'DEFENDER'
            ? 'defender'
            : code === 'MIDFIELDER'
              ? 'midfielder'
              : code === 'ATTACKER'
                ? 'attacker'
                : 'other';
      groups[key].push(m);
    }
    return (
      [
        { key: 'goalkeeper', i18nKey: 'team.squad.gk', defaultLabel: 'KALECİ', members: groups.goalkeeper },
        { key: 'defender', i18nKey: 'team.squad.def', defaultLabel: 'DEFANS', members: groups.defender },
        { key: 'midfielder', i18nKey: 'team.squad.mid', defaultLabel: 'ORTA SAHA', members: groups.midfielder },
        { key: 'attacker', i18nKey: 'team.squad.fwd', defaultLabel: 'FORVET', members: groups.attacker },
        { key: 'other', i18nKey: 'team.squad.other', defaultLabel: 'DİĞER', members: groups.other },
      ] as Bucket[]
    ).filter((b) => b.members.length > 0);
  }, [squad]);

  if (buckets.length === 0) return null;

  return (
    <View style={styles.container}>
      {buckets.map((bucket) => (
        <View
          key={bucket.key}
          style={[styles.card, { backgroundColor: c.surface, borderColor: c.border }]}>
          <View style={styles.sectionHeader}>
            <ThemedText style={[styles.sectionTitle, { color: c.textMuted }]}>
              {t(bucket.i18nKey, { defaultValue: bucket.defaultLabel })}
            </ThemedText>
            <ThemedText style={[styles.sectionCount, { color: c.textMuted }]}>
              {bucket.members.length}
            </ThemedText>
          </View>
          {bucket.members.map((m, i) => (
            <PlayerRow
              key={m.player_id}
              member={m}
              onPress={onPlayerPress}
              first={i === 0}
            />
          ))}
        </View>
      ))}
    </View>
  );
}

function PlayerRow({
  member,
  onPress,
  first,
}: {
  member: TeamSquadMember;
  onPress?: (id: number) => void;
  first: boolean;
}) {
  const c = useTheme();
  const age = ageFromDob(member.date_of_birth);
  const displayName = member.display_name ?? member.name;
  return (
    <Pressable
      onPress={() => onPress?.(member.player_id)}
      android_ripple={{ color: c.brandSoft }}
      style={({ pressed }) => [
        styles.row,
        !first && { borderTopColor: c.border, borderTopWidth: StyleSheet.hairlineWidth },
        pressed && { backgroundColor: c.brandSoft },
      ]}>
      <View style={[styles.jersey, { backgroundColor: c.bg, borderColor: c.border }]}>
        <ThemedText style={[styles.jerseyText, { color: c.text }]}>
          {member.jersey_number ?? '–'}
        </ThemedText>
      </View>
      {member.image_path ? (
        <Image
          source={{ uri: member.image_path }}
          style={styles.avatar}
          contentFit="cover"
        />
      ) : (
        <View
          style={[
            styles.avatar,
            styles.avatarFallback,
            { backgroundColor: c.bg, borderColor: c.border },
          ]}
        />
      )}
      <View style={styles.info}>
        <View style={styles.nameRow}>
          <ThemedText
            style={[styles.name, { color: c.text }]}
            numberOfLines={1}>
            {displayName}
          </ThemedText>
          {member.captain ? (
            <View style={[styles.captainBadge, { backgroundColor: c.brandSoft, borderColor: c.brand }]}>
              <ThemedText style={[styles.captainText, { color: c.brand }]}>
                C
              </ThemedText>
            </View>
          ) : null}
        </View>
        {age != null ? (
          <ThemedText style={[styles.sub, { color: c.textMuted }]}>
            {age}
          </ThemedText>
        ) : null}
      </View>
    </Pressable>
  );
}

function ageFromDob(dob: string | null): number | null {
  if (!dob) return null;
  const ts = Date.parse(dob);
  if (Number.isNaN(ts)) return null;
  const diff = Date.now() - ts;
  // 365.25 averages the leap years; off-by-one on a birthday but the
  // squad list is a glance, not an HR record.
  const years = Math.floor(diff / (365.25 * 24 * 60 * 60 * 1000));
  if (years < 10 || years > 60) return null;
  return years;
}

const styles = StyleSheet.create({
  container: {
    gap: 12,
    paddingTop: 4,
  },
  card: {
    marginHorizontal: 16,
    borderWidth: StyleSheet.hairlineWidth,
    borderRadius: 12,
    overflow: 'hidden',
  },
  sectionHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 14,
    paddingTop: 10,
    paddingBottom: 6,
  },
  sectionTitle: {
    fontSize: 11,
    fontWeight: '800',
    letterSpacing: 0.6,
  },
  sectionCount: {
    fontSize: 11,
    fontWeight: '700',
    fontVariant: ['tabular-nums'],
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 14,
    paddingVertical: 8,
    gap: 10,
  },
  jersey: {
    width: 30,
    height: 30,
    borderRadius: 6,
    borderWidth: StyleSheet.hairlineWidth,
    alignItems: 'center',
    justifyContent: 'center',
  },
  jerseyText: {
    fontSize: 13,
    fontWeight: '700',
    fontVariant: ['tabular-nums'],
  },
  avatar: {
    width: 34,
    height: 34,
    borderRadius: 17,
  },
  avatarFallback: {
    borderWidth: StyleSheet.hairlineWidth,
  },
  info: {
    flex: 1,
    gap: 1,
  },
  nameRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
  },
  name: {
    flex: 1,
    fontSize: 14,
    fontWeight: '600',
  },
  captainBadge: {
    width: 18,
    height: 18,
    borderRadius: 9,
    borderWidth: StyleSheet.hairlineWidth,
    alignItems: 'center',
    justifyContent: 'center',
  },
  captainText: {
    fontSize: 10,
    fontWeight: '900',
  },
  sub: {
    fontSize: 11,
  },
});
