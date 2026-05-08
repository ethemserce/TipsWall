import { Image } from 'expo-image';
import { StyleSheet, View } from 'react-native';

const logoSource = require('@/assets/images/logo.png');

export function AppBrand() {
  return (
    <View style={styles.row}>
      <Image source={logoSource} style={styles.logo} contentFit="contain" />
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
