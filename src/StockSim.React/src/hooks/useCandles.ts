import { useMemo } from "react";
import type { HistoryPoint } from "./useQuotes";

export type Candle = { t: number; open: number; high: number; low: number; close: number };

export function useCandles(points: HistoryPoint[] | undefined, bucketMs = 5000, maxBuckets = 120) {
  return useMemo<Candle[]>(() => {
    const p = points ?? [];
    if (p.length === 0) return [];
    const byBucket = new Map<number, Candle>();
    for (const { t, p: price } of p) {
      const key = Math.floor(t / bucketMs) * bucketMs;
      const c = byBucket.get(key);
      if (!c) {
        byBucket.set(key, { t: key, open: price, high: price, low: price, close: price });
      } else {
        c.high = Math.max(c.high, price);
        c.low = Math.min(c.low, price);
        c.close = price;
      }
    }
    const arr = Array.from(byBucket.values()).sort((a, b) => a.t - b.t);
    return arr.slice(-maxBuckets);
  }, [points, bucketMs, maxBuckets]);
}