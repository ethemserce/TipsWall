import { useTranslation } from 'react-i18next';

import { RateScreen } from '@/src/screens/RateScreen';

export default function HotRateRoute() {
  const { t } = useTranslation();
  return (
    <RateScreen
      kind="hot"
      title={t('rate.titles.hot')}
      primaryMetric="winning_percent"
    />
  );
}
