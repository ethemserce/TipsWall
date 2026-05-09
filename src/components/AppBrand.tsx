import { Image } from 'expo-image';
import { StyleSheet, View } from 'react-native';

import { useEffectiveScheme } from '@/src/lib/settings/useEffectiveScheme';

// Two raster variants: white glyph for dark backgrounds, black glyph for
// light. Swapping by active scheme keeps the brand legible across themes
// AND honours the manual theme override from Settings.
const logoLight = require('@/assets/images/logo-black.png');
const logoDark = require('@/assets/images/logo.png');

export function AppBrand() {
  const scheme = useEffectiveScheme();
  return (
    <View style={styles.row}>
      <Image
        source={scheme === 'dark' ? logoDark : logoLight}
        style={styles.logo}
        contentFit="contain"
      />
    </View>
  );
}

const styles = StyleSheet.create({
  row: {
    width: '100%',
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
  },
  logo: {
    width: 160,
    height: 40,
  },
});
