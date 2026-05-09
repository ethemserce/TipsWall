import { useLocalSearchParams } from 'expo-router';

import { TeamDetailScreen } from '@/src/screens/TeamDetailScreen';

export default function TeamRoute() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const teamId = Number(id);
  if (!Number.isFinite(teamId)) return null;
  return <TeamDetailScreen teamId={teamId} />;
}
