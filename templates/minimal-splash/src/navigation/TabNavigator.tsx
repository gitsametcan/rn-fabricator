import React from 'react';
import { SafeAreaView, StyleSheet, Text, View } from 'react-native';
import { TAB_ROUTES } from './routes';

export function TabNavigator() {
  return (
    <SafeAreaView style={styles.safeArea}>
      <View style={styles.container}>
        <Text style={styles.title}>Tabs</Text>
        {TAB_ROUTES.map(route => (
          <View key={route.name} style={styles.item}>
            <Text style={styles.itemTitle}>{route.title}</Text>
            <Text style={styles.itemName}>{route.name}</Text>
          </View>
        ))}
      </View>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safeArea: {
    flex: 1,
    backgroundColor: '#F8FAFC',
  },
  container: {
    flex: 1,
    gap: 12,
    padding: 24,
  },
  title: {
    color: '#111827',
    fontSize: 28,
    fontWeight: '700',
    marginBottom: 8,
  },
  item: {
    backgroundColor: '#FFFFFF',
    borderColor: '#E5E7EB',
    borderRadius: 8,
    borderWidth: 1,
    padding: 16,
  },
  itemTitle: {
    color: '#111827',
    fontSize: 16,
    fontWeight: '700',
  },
  itemName: {
    color: '#6B7280',
    fontSize: 13,
    marginTop: 4,
  },
});
