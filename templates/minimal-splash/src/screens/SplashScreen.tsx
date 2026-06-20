import React from 'react';
import { SafeAreaView, StatusBar, StyleSheet, Text, View } from 'react-native';

export function SplashScreen() {
  return (
    <SafeAreaView style={styles.safeArea}>
      <StatusBar barStyle="dark-content" />
      <View style={styles.container}>
        <Text style={styles.brand}>rn-fabricator</Text>
        <Text style={styles.title}>Your React Native app is ready.</Text>
        <Text style={styles.subtitle}>Start from this minimal splash screen.</Text>
      </View>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safeArea: {
    flex: 1,
    backgroundColor: '#F7F8FA',
  },
  container: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    padding: 24,
  },
  brand: {
    color: '#2563EB',
    fontSize: 16,
    fontWeight: '700',
    marginBottom: 16,
  },
  title: {
    color: '#111827',
    fontSize: 28,
    fontWeight: '700',
    textAlign: 'center',
  },
  subtitle: {
    color: '#4B5563',
    fontSize: 16,
    marginTop: 12,
    textAlign: 'center',
  },
});
