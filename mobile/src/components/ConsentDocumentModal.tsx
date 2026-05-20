import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Modal,
  type NativeScrollEvent,
  type NativeSyntheticEvent,
  Pressable,
  ScrollView,
  StyleSheet,
  View,
} from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import { ThemedText } from '@/components/themed-text';
import { getLegalDoc, type LegalTopic } from '@/src/lib/legal/content';
import { useTheme } from '@/src/lib/useTheme';

interface ConsentDocumentModalProps {
  visible: boolean;
  topic: LegalTopic;
  onAccept: () => void;
  onClose: () => void;
}

// Slack the bottom check by this many pixels — captures "user got to the
// last paragraph" without demanding pixel-perfect end-of-scroll, which
// is brittle when contentSize has fractional rendering on Android and
// iOS settles slightly above the math bottom after bounce-overscroll.
const BOTTOM_THRESHOLD_PX = 48;

/**
 * Full-screen modal that surfaces a legal document the user must read
 * end-to-end before they can accept. Pattern shipped for the signup
 * screen's KVKK / Terms consent flow — the user taps a link, the doc
 * scrolls inside this modal, and the bottom "Kabul Ediyorum" button
 * only enables after their scroll reaches the end of the content.
 *
 * Bottom-reach detection uses onScroll's contentOffset + visible
 * height + content size, which is more robust than measuring children
 * because it lets short docs (where the whole text fits without
 * scrolling) auto-enable the accept button immediately via the initial
 * onContentSizeChange callback.
 */
export function ConsentDocumentModal({
  visible,
  topic,
  onAccept,
  onClose,
}: ConsentDocumentModalProps) {
  const c = useTheme();
  const { t, i18n } = useTranslation();
  const insets = useSafeAreaInsets();
  const doc = getLegalDoc(topic, i18n.language);
  const [reachedBottom, setReachedBottom] = useState(false);

  // Reset to false whenever the modal is reopened so the previous
  // session's "read" state doesn't carry over.
  const handleOpen = () => {
    setReachedBottom(false);
  };

  const checkBottom = (
    contentHeight: number,
    layoutHeight: number,
    offsetY: number,
  ) => {
    if (reachedBottom) return;
    // Short docs that fit entirely on-screen auto-pass.
    if (contentHeight <= layoutHeight + BOTTOM_THRESHOLD_PX) {
      setReachedBottom(true);
      return;
    }
    if (offsetY + layoutHeight >= contentHeight - BOTTOM_THRESHOLD_PX) {
      setReachedBottom(true);
    }
  };

  const handleScroll = (e: NativeSyntheticEvent<NativeScrollEvent>) => {
    const { contentSize, layoutMeasurement, contentOffset } = e.nativeEvent;
    checkBottom(contentSize.height, layoutMeasurement.height, contentOffset.y);
  };

  // Backstop for the throttled onScroll: when the user finishes a swipe
  // gesture (drag end) or the momentum animation settles (momentum end),
  // re-check the position. This catches the iOS case where the last
  // throttled scroll event lands just before bounce-overscroll settle,
  // leaving offset slightly below the bottom mathematically even though
  // the user is at the visual end.
  const handleScrollSettle = (e: NativeSyntheticEvent<NativeScrollEvent>) => {
    const { contentSize, layoutMeasurement, contentOffset } = e.nativeEvent;
    checkBottom(contentSize.height, layoutMeasurement.height, contentOffset.y);
  };

  return (
    <Modal
      visible={visible}
      transparent
      animationType="slide"
      onRequestClose={onClose}
      onShow={handleOpen}>
      <View style={[styles.flex, { backgroundColor: c.bg }]}>
        <View
          style={[
            styles.headerRow,
            { borderBottomColor: c.borderSoft, paddingTop: insets.top + 8 },
          ]}>
          <Pressable
            onPress={onClose}
            hitSlop={12}
            style={({ pressed }) => [
              styles.iconBtn,
              { backgroundColor: pressed ? c.brandSoft : 'transparent' },
            ]}>
            <MaterialCommunityIcons name="close" size={24} color={c.text} />
          </Pressable>
          <ThemedText
            style={[styles.headerTitle, { color: c.text }]}
            numberOfLines={1}>
            {doc.title}
          </ThemedText>
          <View style={styles.iconBtn} />
        </View>

        <ScrollView
          contentContainerStyle={styles.scroll}
          onScroll={handleScroll}
          onMomentumScrollEnd={handleScrollSettle}
          onScrollEndDrag={handleScrollSettle}
          scrollEventThrottle={16}>
          <ThemedText style={[styles.updated, { color: c.textMuted }]}>
            {doc.lastUpdated}
          </ThemedText>
          {doc.paragraphs.map((p, idx) => {
            if (p.startsWith('# ')) {
              return (
                <ThemedText
                  key={idx}
                  style={[styles.heading, { color: c.text }]}>
                  {p.slice(2)}
                </ThemedText>
              );
            }
            return (
              <ThemedText key={idx} style={[styles.body, { color: c.textMuted }]}>
                {p}
              </ThemedText>
            );
          })}
        </ScrollView>

        <View
          style={[
            styles.footer,
            {
              borderTopColor: c.borderSoft,
              backgroundColor: c.surface,
              paddingBottom: insets.bottom + 12,
            },
          ]}>
          {!reachedBottom ? (
            <ThemedText style={[styles.scrollHint, { color: c.textMuted }]}>
              {t('auth.consent.scrollHint')}
            </ThemedText>
          ) : null}
          <Pressable
            onPress={() => {
              if (!reachedBottom) return;
              onAccept();
            }}
            disabled={!reachedBottom}
            style={({ pressed }) => [
              styles.acceptBtn,
              {
                backgroundColor: reachedBottom ? c.brand : c.border,
                opacity: pressed && reachedBottom ? 0.85 : 1,
              },
            ]}>
            <ThemedText
              style={[styles.acceptBtnText, { color: c.textInverse }]}>
              {t('auth.consent.acceptBtn')}
            </ThemedText>
          </Pressable>
        </View>
      </View>
    </Modal>
  );
}

const styles = StyleSheet.create({
  flex: { flex: 1 },
  headerRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 8,
    paddingBottom: 8,
    borderBottomWidth: StyleSheet.hairlineWidth,
  },
  iconBtn: {
    width: 36,
    height: 36,
    borderRadius: 18,
    alignItems: 'center',
    justifyContent: 'center',
  },
  headerTitle: {
    flex: 1,
    textAlign: 'center',
    fontSize: 16,
    fontWeight: '800',
  },
  scroll: {
    paddingHorizontal: 20,
    paddingTop: 12,
    paddingBottom: 32,
    gap: 6,
  },
  updated: {
    fontSize: 11,
    marginBottom: 12,
  },
  heading: {
    fontSize: 14,
    fontWeight: '800',
    marginTop: 14,
    marginBottom: 2,
    letterSpacing: 0.2,
  },
  body: {
    fontSize: 13,
    lineHeight: 19,
  },
  footer: {
    paddingHorizontal: 20,
    paddingTop: 12,
    borderTopWidth: StyleSheet.hairlineWidth,
    gap: 8,
  },
  scrollHint: {
    fontSize: 11,
    textAlign: 'center',
    letterSpacing: 0.2,
  },
  acceptBtn: {
    paddingVertical: 14,
    borderRadius: 12,
    alignItems: 'center',
    justifyContent: 'center',
  },
  acceptBtnText: {
    fontSize: 14,
    fontWeight: '800',
    letterSpacing: 0.4,
  },
});
