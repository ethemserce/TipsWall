import { RateScreen } from '@/src/screens/RateScreen';

export default function EarningRateRoute() {
  return (
    <RateScreen kind="earning" title="Earning Rate" primaryMetric="earning_percent" />
  );
}
