import type { ReactNode } from 'react';
import { Pressable, StyleSheet, Text } from 'react-native';

export type PrimaryButtonProps = {
  children: ReactNode;
  disabled?: boolean;
  onPress?: () => void;
};

export function PrimaryButton({
  children,
  disabled = false,
  onPress,
}: PrimaryButtonProps) {
  return (
    <Pressable
      accessibilityRole="button"
      disabled={disabled}
      onPress={onPress}
      style={({ pressed }) => [
        styles.button,
        pressed && !disabled ? styles.pressed : null,
        disabled ? styles.disabled : null,
      ]}>
      <Text style={styles.label}>{children}</Text>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  button: {
    alignItems: 'center',
    backgroundColor: '#2563eb',
    borderRadius: 8,
    minHeight: 48,
    justifyContent: 'center',
    paddingHorizontal: 18,
  },
  pressed: {
    backgroundColor: '#1d4ed8',
  },
  disabled: {
    backgroundColor: '#9ca3af',
  },
  label: {
    color: '#ffffff',
    fontSize: 16,
    fontWeight: '700',
  },
});
