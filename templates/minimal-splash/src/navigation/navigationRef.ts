import type { AppRouteName, NavigationCommand } from './types';

type NavigationListener = (command: NavigationCommand) => void;

let currentRoute: AppRouteName | null = null;
const listeners = new Set<NavigationListener>();

export const navigationRef = {
  getCurrentRoute() {
    return currentRoute;
  },
  navigate(name: AppRouteName, params?: Record<string, unknown>) {
    currentRoute = name;
    const command: NavigationCommand = params === undefined ? { name } : { name, params };

    listeners.forEach(listener => listener(command));
  },
  subscribe(listener: NavigationListener) {
    listeners.add(listener);

    return () => listeners.delete(listener);
  },
};
