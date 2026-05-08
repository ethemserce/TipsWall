// In-memory AsyncStorage mock for Jest. Matches the subset of the API the
// app actually uses (getItem, setItem, removeItem, multiGet not yet needed).
// Each test run starts with a fresh map because Jest re-imports per file.

const store = new Map<string, string>();

export default {
  async getItem(key: string): Promise<string | null> {
    return store.has(key) ? store.get(key)! : null;
  },
  async setItem(key: string, value: string): Promise<void> {
    store.set(key, value);
  },
  async removeItem(key: string): Promise<void> {
    store.delete(key);
  },
  async clear(): Promise<void> {
    store.clear();
  },
  async multiGet(keys: string[]): Promise<[string, string | null][]> {
    return keys.map((k) => [k, store.has(k) ? store.get(k)! : null]);
  },
};
