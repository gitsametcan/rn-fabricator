import { Pressable, SafeAreaView, StyleSheet, Text, View } from 'react-native';

export type MainMenuAction = {
  id: string;
  label: string;
  description?: string;
  onPress?: () => void;
};

export type MainMenuScreenProps = {
  title?: string;
  actions?: MainMenuAction[];
};

const defaultActions: MainMenuAction[] = [
  {
    id: 'dashboard',
    label: 'Dashboard',
    description: 'Open the main dashboard.',
  },
  {
    id: 'settings',
    label: 'Settings',
    description: 'Review application settings.',
  },
];

export function MainMenuScreen({
  title = 'Menu',
  actions = defaultActions,
}: MainMenuScreenProps) {
  return (
    <SafeAreaView style={styles.safeArea}>
      <View style={styles.container}>
        <Text style={styles.title}>{title}</Text>
        <View style={styles.actions}>
          {actions.map(action => (
            <Pressable
              accessibilityRole="button"
              key={action.id}
              onPress={action.onPress}
              style={styles.action}>
              <Text style={styles.actionLabel}>{action.label}</Text>
              {action.description ? (
                <Text style={styles.actionDescription}>{action.description}</Text>
              ) : null}
            </Pressable>
          ))}
        </View>
      </View>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safeArea: {
    flex: 1,
    backgroundColor: '#f8fafc',
  },
  container: {
    flex: 1,
    gap: 20,
    padding: 24,
  },
  title: {
    color: '#111827',
    fontSize: 28,
    fontWeight: '700',
  },
  actions: {
    gap: 12,
  },
  action: {
    backgroundColor: '#ffffff',
    borderColor: '#e5e7eb',
    borderRadius: 8,
    borderWidth: 1,
    padding: 16,
  },
  actionLabel: {
    color: '#111827',
    fontSize: 16,
    fontWeight: '600',
  },
  actionDescription: {
    color: '#6b7280',
    fontSize: 14,
    marginTop: 4,
  },
});
