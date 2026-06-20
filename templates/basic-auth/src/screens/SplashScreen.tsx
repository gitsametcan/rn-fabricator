import React from 'react';
import { StyleSheet, Text, View } from 'react-native';

export function SplashScreen() {
  return (
    <View style={styles.container}>
      <View style={styles.logoMark}>
        <Text style={styles.logoText}>RF</Text>
      </View>
      <Text style={styles.title}>rn-fabricator</Text>
      <Text style={styles.subtitle}>Preparing your workspace</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    alignItems: 'center',
    backgroundColor: '#111827',
    flex: 1,
    justifyContent: 'center',
    padding: 24,
  },
  logoMark: {
    alignItems: 'center',
    backgroundColor: '#38bdf8',
    borderRadius: 20,
    height: 72,
    justifyContent: 'center',
    marginBottom: 24,
    width: 72,
  },
  logoText: {
    color: '#0f172a',
    fontSize: 24,
    fontWeight: '700',
  },
  subtitle: {
    color: '#cbd5e1',
    fontSize: 16,
    marginTop: 8,
    textAlign: 'center',
  },
  title: {
    color: '#f8fafc',
    fontSize: 28,
    fontWeight: '700',
  },
});
