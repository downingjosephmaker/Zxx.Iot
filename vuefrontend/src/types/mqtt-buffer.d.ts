import type { Buffer } from "buffer";

declare global {
  interface Window {
    Buffer: typeof Buffer;
    process: {
      env: Record<string, any>;
      nextTick: (fn: Function, ...args: any[]) => void;
    };
    global: Window;
  }
}

export {};
