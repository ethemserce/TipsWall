import { useTranslation } from 'react-i18next';

import { RateScreen } from '@/src/screens/RateScreen';

export default function WinningRateRoute() {
  const { t } = useTranslation();
  return (
    <RateScreen
      kind="winning"
      title={t('rate.titles.winning')}
      primaryMetric="winning_percent"
    />
  );
}
