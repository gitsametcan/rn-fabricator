import { Pressable, SafeAreaView, StyleSheet, Switch, Text, View } from 'react-native';

export type SettingsItem = {
  id: string;
  title: string;
  description?: string;
  type?: 'toggle' | 'action';
  value?: boolean;
  onPress?: () => void;
  onValueChange?: (value: boolean) => void;
};

export type SettingsScreenProps = {
  title?: string;
  items?: SettingsItem[];
};

const defaultItems: SettingsItem[] = [
  {
    id: 'notifications',
    title: 'Notifications',
    description: 'Receive important product updates.',
    type: 'toggle',
    value: true,
  },
  {
    id: 'profile',
    title: 'Profile',
    description: 'Review account details.',
    type: 'action',
  },
  {
    id: 'security',
    title: 'Security',
    description: 'Manage sign-in and device settings.',
    type: 'action',
  },
];

export function SettingsScreen({
  title = 'Settings',
  items = defaultItems,
}: SettingsScreenProps) {
  return (
    <SafeAreaView style={styles.safeArea}>
      <View style={styles.container}>
        <Text style={styles.title}>{title}</Text>
        <View style={styles.list}>
          {items.map(item => (
            <Pressable
              accessibilityRole={item.type === 'toggle' ? 'switch' : 'button'}
              key={item.id}
              onPress={item.onPress}
              style={styles.item}>
              <View style={styles.itemContent}>
                <Text style={styles.itemTitle}>{item.title}</Text>
                {item.description ? (
                  <Text style={styles.itemDescription}>{item.description}</Text>
                ) : null}
              </View>
              {item.type === 'toggle' ? (
                <Switch
                  onValueChange={item.onValueChange}
                  value={item.value ?? false}
                />
              ) : (
                <Text style={styles.chevron}>›</Text>
              )}
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
    backgroundColor: '#F8FAFC',
  },
  container: {
    flex: 1,
    padding: 24,
  },
  title: {
    color: '#111827',
    fontSize: 28,
    fontWeight: '700',
    marginBottom: 20,
  },
  list: {
    gap: 12,
  },
  item: {
    alignItems: 'center',
    backgroundColor: '#FFFFFF',
    borderColor: '#E5E7EB',
    borderRadius: 8,
    borderWidth: 1,
    flexDirection: 'row',
    gap: 16,
    justifyContent: 'space-between',
    minHeight: 72,
    paddingHorizontal: 16,
    paddingVertical: 14,
  },
  itemContent: {
    flex: 1,
  },
  itemTitle: {
    color: '#111827',
    fontSize: 16,
    fontWeight: '700',
  },
  itemDescription: {
    color: '#6B7280',
    fontSize: 14,
    lineHeight: 20,
    marginTop: 4,
  },
  chevron: {
    color: '#9CA3AF',
    fontSize: 28,
    lineHeight: 28,
  },
});
