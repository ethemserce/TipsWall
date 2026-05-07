import { StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { useTheme } from '@/src/lib/useTheme';

export function AppBrand() {
  const c = useTheme();
  return (
    <View style={styles.row}>
      <View style={[styles.logo, { backgroundColor: c.brand }]}>
        <ThemedText style={[styles.logoChar, { color: c.textInverse }]}>T</ThemedText>
      </View>
      <ThemedText style={[styles.name, { color: c.text }]}>TipsWall</ThemedText>
    </View>
  );
}

const styles = StyleSheet.create({
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  logo: {
    width: 28,
    height: 28,
    borderRadius: 8,
    alignItems: 'center',
    justifyContent: 'center',
  },
  logoChar: {
    fontSize: 16,
    fontWeight: '800',
    letterSpacing: 0.5,
  },
  name: {
    fontSize: 18,
    fontWeight: '700',
    letterSpacing: 0.3,
  },
});
