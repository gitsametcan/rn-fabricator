import React from 'react';
import { ActivityIndicator, StyleSheet, Text, View } from 'react-native';

type LoadingScreenProps = {
  message?: string;
};

export function LoadingScreen({ message = 'Loading your session' }: LoadingScreenProps) {
  return (
    <View style={styles.container}>
      <ActivityIndicator color="#2563eb" size="large" />
      <Text style={styles.message}>{message}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    alignItems: 'center',
    backgroundColor: '#f8fafc',
    flex: 1,
    justifyContent: 'center',
    padding: 24,
  },
  message: {
    color: '#334155',
    fontSize: 16,
    marginTop: 16,
    textAlign: 'center',
  },
});
