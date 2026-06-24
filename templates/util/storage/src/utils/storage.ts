export type KeyValueStorage = {
  getItem: (key: string) => Promise<string | null>;
  setItem: (key: string, value: string) => Promise<void>;
  removeItem: (key: string) => Promise<void>;
};

export async function readJson<TValue>(
  storage: KeyValueStorage,
  key: string,
): Promise<TValue | null> {
  const rawValue = await storage.getItem(key);

  if (rawValue === null) {
    return null;
  }

  return JSON.parse(rawValue) as TValue;
}

export async function writeJson<TValue>(
  storage: KeyValueStorage,
  key: string,
  value: TValue,
): Promise<void> {
  await storage.setItem(key, JSON.stringify(value));
}

export async function removeValue(
  storage: KeyValueStorage,
  key: string,
): Promise<void> {
  await storage.removeItem(key);
}
