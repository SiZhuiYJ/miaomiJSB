export type BrowserProcess = {
  browser: boolean;
  env: Record<string, string | undefined>;
  argv: string[];
  version: string;
  versions: Record<string, string>;
  platform: string;
  cwd: () => string;
  nextTick: (callback: (...args: unknown[]) => void, ...args: unknown[]) => void;
  on: () => BrowserProcess;
  addListener: () => BrowserProcess;
  once: () => BrowserProcess;
  off: () => BrowserProcess;
  removeListener: () => BrowserProcess;
  removeAllListeners: () => BrowserProcess;
  emit: () => boolean;
};

const createBrowserProcess = (): BrowserProcess => {
  const processShim = {
    browser: true,
    env: {
      NODE_ENV: import.meta.env.PROD ? 'production' : 'development',
    },
    argv: [],
    version: '',
    versions: {},
    platform: 'browser',
    cwd: () => '/',
    nextTick: (callback: (...args: unknown[]) => void, ...args: unknown[]) => {
      queueMicrotask(() => callback(...args));
    },
    on: () => processShim,
    addListener: () => processShim,
    once: () => processShim,
    off: () => processShim,
    removeListener: () => processShim,
    removeAllListeners: () => processShim,
    emit: () => false,
  } satisfies BrowserProcess;

  return processShim;
};

export const browserProcess = createBrowserProcess();
