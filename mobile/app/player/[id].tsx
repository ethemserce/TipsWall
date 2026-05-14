import { useLocalSearchParams } from 'expo-router';

import { PlayerDetailScreen } from '@/src/screens/PlayerDetailScreen';

export default function PlayerRoute() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const playerId = Number(id);
  if (!Number.isFinite(playerId)) return null;
  return <PlayerDetailScreen playerId={playerId} />;
}
