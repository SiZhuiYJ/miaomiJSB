/// <reference types="vite/client" />

declare global {
  interface Window {
    Buffer: typeof import('buffer').Buffer;
    process: NodeJS.Process;
  }

  // Legacy decrypt modules and their browserified dependencies use Node globals.
  var Buffer: typeof import('buffer').Buffer;
}

export {};
