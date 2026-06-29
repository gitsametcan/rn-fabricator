import type { ReactNode } from 'react';
import { SafeAreaView, StyleSheet, Text, View } from 'react-native';

export type AppShellProps = {
  title: string;
  children: ReactNode;
  footer?: ReactNode;
};

export function AppShell({ title, children, footer }: AppShellProps) {
  return (
    <SafeAreaView style={styles.safeArea}>
      <View style={styles.header}>
        <Text style={styles.title}>{title}</Text>
      </View>
      <View style={styles.content}>{children}</View>
      {footer ? <View style={styles.footer}>{footer}</View> : null}
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safeArea: {
    flex: 1,
    backgroundColor: '#ffffff',
  },
  header: {
    borderBottomColor: '#e5e7eb',
    borderBottomWidth: 1,
    paddingHorizontal: 20,
    paddingVertical: 16,
  },
  title: {
    color: '#111827',
    fontSize: 20,
    fontWeight: '700',
  },
  content: {
    flex: 1,
    padding: 20,
  },
  footer: {
    borderTopColor: '#e5e7eb',
    borderTopWidth: 1,
    padding: 16,
  },
});
