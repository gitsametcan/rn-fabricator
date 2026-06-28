import React from 'react';
import { SafeAreaView, StyleSheet, Text, View } from 'react-native';
import { ROUTES } from './routes';

export function AuthNavigator() {
  return (
    <SafeAreaView style={styles.safeArea}>
      <View style={styles.container}>
        <Text style={styles.eyebrow}>{ROUTES.auth.signIn}</Text>
        <Text style={styles.title}>Auth flow</Text>
        <Text style={styles.subtitle}>Add login, register, and password recovery screens here.</Text>
      </View>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safeArea: {
    flex: 1,
    backgroundColor: '#FFFFFF',
  },
  container: {
    flex: 1,
    justifyContent: 'center',
    padding: 24,
  },
  eyebrow: {
    color: '#2563EB',
    fontSize: 14,
    fontWeight: '700',
    marginBottom: 12,
    textTransform: 'uppercase',
  },
  title: {
    color: '#111827',
    fontSize: 28,
    fontWeight: '700',
  },
  subtitle: {
    color: '#4B5563',
    fontSize: 16,
    lineHeight: 24,
    marginTop: 12,
  },
});
