export {};

declare global {
  interface Window {
    __env?: {
      VITE_MARKETFEED_URL?: string;
    };
  }
}
