// Bumps Gradle daemon JVM heap + metaspace so KSP-heavy Kotlin tasks
// (expo-updates:kspReleaseKotlin, expo-modules-core:lintVitalAnalyzeRelease)
// don't crash with `OutOfMemoryError: Metaspace` on stock 512m metaspace.
//
// Expo managed builds regenerate android/gradle.properties on every prebuild,
// so editing it by hand doesn't survive. This plugin runs as a `withGradleProperties`
// mod and rewrites (or appends) the `org.gradle.jvmargs` entry at prebuild
// time, after Expo has written the template defaults.
//
// Numbers picked for a 16 GB host with WSL2 default split (~8 GB to Linux):
//   -Xmx4g            heap is enough for R8 + Metro bundling
//   -XX:MaxMetaspaceSize=1g  fixes the KSP crash (default ~512m is too tight)
//   -Dfile.encoding=UTF-8    keeps emoji / accented strings in resource files
//
// If a future build still OOMs on a beefier task (R8, dex), bump heap to 6g.

const { withGradleProperties } = require('@expo/config-plugins');

const JVM_ARGS = '-Xmx4096m -XX:MaxMetaspaceSize=1024m -Dfile.encoding=UTF-8';

module.exports = function withGradleJvmMemory(config) {
  return withGradleProperties(config, (cfg) => {
    const props = cfg.modResults;
    const idx = props.findIndex(
      (p) => p.type === 'property' && p.key === 'org.gradle.jvmargs',
    );
    if (idx >= 0) {
      props[idx].value = JVM_ARGS;
    } else {
      props.push({ type: 'property', key: 'org.gradle.jvmargs', value: JVM_ARGS });
    }
    return cfg;
  });
};
