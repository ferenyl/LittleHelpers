import { vi } from 'vitest';

// Provide a localStorage implementation for Node/Vitest test environments
const localStorageMock = (() => {
  let store: Record<string, string> = {};
  return {
    getItem: (key: string) => store[key] ?? null,
    setItem: (key: string, value: string) => { store[key] = String(value); },
    removeItem: (key: string) => { delete store[key]; },
    clear: () => { store = {}; },
    get length() { return Object.keys(store).length; },
    key: (i: number) => Object.keys(store)[i] ?? null,
  };
})();

vi.stubGlobal('localStorage', localStorageMock);

// Provide a browser-like matchMedia for Vitest/jsdom
vi.stubGlobal('matchMedia', (query: string) => ({
  matches: false,
  media: query,
  onchange: null,
  addListener: () => {},
  removeListener: () => {},
  addEventListener: () => {},
  removeEventListener: () => {},
  dispatchEvent: () => false,
}));

vi.stubGlobal('Notification', {
  permission: 'default',
  requestPermission: vi.fn(async () => 'granted'),
});

Object.defineProperty(globalThis.navigator, 'serviceWorker', {
  value: {
    register: vi.fn(async () => ({
      showNotification: vi.fn(async () => undefined),
    })),
  },
  configurable: true,
});
