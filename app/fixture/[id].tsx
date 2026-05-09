import { useLocalSearchParams } from 'expo-router';

import { FixtureDetailScreen } from '@/src/screens/FixtureDetailScreen';

export default function FixtureRoute() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const fixtureId = Number(id);
  if (!Number.isFinite(fixtureId)) return null;
  return <FixtureDetailScreen fixtureId={fixtureId} />;
}
