import { useTranslation } from 'react-i18next';

import { RateScreen } from '@/src/screens/RateScreen';

export default function EarningRateRoute() {
  const { t } = useTranslation();
  return (
    <RateScreen
      kind="earning"
      title={t('rate.titles.earning')}
      primaryMetric="earning_percent"
    />
  );
}
