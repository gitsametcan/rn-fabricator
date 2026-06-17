import React from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';

type HomeScreenProps = {
  displayName?: string;
  onSignOut?: () => void;
};

export function HomeScreen({ displayName = 'Developer', onSignOut }: HomeScreenProps) {
  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.eyebrow}>Signed in</Text>
        <Text style={styles.title}>Hello, {displayName}</Text>
        <Text style={styles.subtitle}>Your basic auth starter is ready for app-specific features.</Text>
      </View>

      <View style={styles.summaryBox}>
        <Text style={styles.summaryTitle}>Starter checklist</Text>
        <Text style={styles.summaryItem}>- Replace placeholder auth with your backend.</Text>
        <Text style={styles.summaryItem}>- Add navigation once app routes are defined.</Text>
        <Text style={styles.summaryItem}>- Move secrets to environment-specific config.</Text>
      </View>

      <Pressable accessibilityRole="button" onPress={onSignOut} style={styles.signOutButton}>
        <Text style={styles.signOutText}>Sign out</Text>
      </Pressable>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    backgroundColor: '#f8fafc',
    flex: 1,
    justifyContent: 'space-between',
    padding: 24,
    paddingTop: 72,
  },
  eyebrow: {
    color: '#16a34a',
    fontSize: 13,
    fontWeight: '700',
    textTransform: 'uppercase',
  },
  signOutButton: {
    alignItems: 'center',
    borderColor: '#ef4444',
    borderRadius: 8,
    borderWidth: 1,
    paddingHorizontal: 16,
    paddingVertical: 14,
  },
  signOutText: {
    color: '#b91c1c',
    fontSize: 16,
    fontWeight: '700',
  },
  subtitle: {
    color: '#64748b',
    fontSize: 16,
    lineHeight: 24,
    marginTop: 8,
  },
  summaryBox: {
    backgroundColor: '#ffffff',
    borderColor: '#e2e8f0',
    borderRadius: 8,
    borderWidth: 1,
    padding: 18,
  },
  summaryItem: {
    color: '#475569',
    fontSize: 15,
    lineHeight: 24,
  },
  summaryTitle: {
    color: '#0f172a',
    fontSize: 17,
    fontWeight: '700',
    marginBottom: 8,
  },
  title: {
    color: '#0f172a',
    fontSize: 30,
    fontWeight: '700',
    marginTop: 8,
  },
});
