import { useLocalSearchParams } from 'expo-router';

import { LeagueDetailScreen } from '@/src/screens/LeagueDetailScreen';

export default function LeagueRoute() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const leagueId = Number(id);
  if (!Number.isFinite(leagueId)) return null;
  return <LeagueDetailScreen leagueId={leagueId} />;
}
