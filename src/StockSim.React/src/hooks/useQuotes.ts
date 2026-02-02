import { useEffect, useMemo, useRef, useState } from "react";
import * as signalR from "@microsoft/signalr";

export type Quote = { symbol: string; price: number; change: number; timeUtc: string };
export type HistoryPoint = { t: number; p: number };
type RawQuote =
  Partial<Quote> & {
    bid?: number; ask?: number; last?: number | null; ts?: string;
    Symbol?: string; Bid?: number; Ask?: number; Last?: number | null; Ts?: string;
    Price?: number; Change?: number; TimeUtc?: string;
  };

type Options = {
  windowSize?: number;
  baseUrl?: string;
  transport?: signalR.HttpTransportType;
  skipNegotiation?: boolean;
};

export function useQuotes(opts: Options = {}) {
  const {
    windowSize = 240,
    baseUrl = (typeof window !== "undefined" && window.__env?.VITE_MARKETFEED_URL)
      ? window.__env.VITE_MARKETFEED_URL
      : (import.meta.env.VITE_MARKETFEED_URL ?? "http://localhost:8081"),
    transport = signalR.HttpTransportType.WebSockets,
    skipNegotiation = true,
  } = opts;

  const initialRef = useRef<PersistedState | null>(null);
  const ensureInitial = () => {
    if (initialRef.current === null) {
      initialRef.current = loadPersisted(windowSize);
    }
    return initialRef.current;
  };

  const [quotes, setQuotes] = useState<Record<string, Quote>>(() => ensureInitial().quotes);
  const [history, setHistory] = useState<Record<string, HistoryPoint[]>>(() => ensureInitial().history);
  const [status, setStatus] = useState<"disconnected" | "connecting" | "connected" | "reconnecting">("disconnected");
  const [paused, setPaused] = useState(false);

  const hubRef = useRef<signalR.HubConnection | null>(null);
  const pausedRef = useRef<boolean>(paused);
  useEffect(() => { pausedRef.current = paused; }, [paused]);

  useEffect(() => {
    setStatus("connecting");
    const hub = new signalR.HubConnectionBuilder()
      .withUrl(`${baseUrl}/hubs/quotes`, { skipNegotiation, transport })
      .withAutomaticReconnect()
      .build();

    hub.onreconnecting(() => setStatus("reconnecting"));
    hub.onreconnected(() => setStatus("connected"));
    hub.onclose(() => setStatus("disconnected"));

    hub.on("quote", (raw: RawQuote) => {
      const key = readString(raw, ["symbol","Symbol"]);
      console.log("hub quote", raw, "normalized key:", key);
      if (pausedRef.current) return;
      let normalized: Quote | null = null;
      setQuotes(prev => {
        const key = readString(raw, ["symbol", "Symbol"]);
        const next = normalizeQuote(raw, key ? prev[key] : undefined);
        if (!next) return prev;
        normalized = next;
        return { ...prev, [next.symbol]: next };
      });
      if (!normalized) return;
      setHistory(h => {
        const symbol = normalized!.symbol;
        const list = (h[symbol] ?? []).concat({ t: Date.now(), p: normalized!.price });
        return { ...h, [symbol]: list.slice(-windowSize) };
      });
    });

    hub.start()
      .then(() => setStatus("connected"))
      .catch(err => { console.error("Quotes hub start error:", err); setStatus("disconnected"); });

    hubRef.current = hub;

    return () => {
      // Only stop if not already disconnected
      const state = hub.state;
      if (state !== signalR.HubConnectionState.Disconnected) {
        hub.stop().catch(() => void 0);
      }
      hubRef.current = null;
      setStatus("disconnected");
    };
  }, [baseUrl, transport, skipNegotiation, windowSize]); // NOTE: paused removed on purpose

  useEffect(() => {
    if (typeof window === "undefined") return;
    const debounce = window.setTimeout(() => {
      persistState({ quotes, history }, windowSize);
    }, 250);
    return () => window.clearTimeout(debounce);
  }, [quotes, history, windowSize]);

  useEffect(() => {
    if (typeof window === "undefined") return;
    const listener = (ev: StorageEvent) => {
      if (ev.key !== STORAGE_KEY || ev.newValue == null) return;
      const next = parsePersisted(ev.newValue, windowSize);
      setQuotes(next.quotes);
      setHistory(next.history);
    };
    window.addEventListener("storage", listener);
    return () => window.removeEventListener("storage", listener);
  }, [windowSize]);

  const items = useMemo(() => Object.values(quotes).sort((a, b) => a.symbol.localeCompare(b.symbol)), [quotes]);
  const topGainers = useMemo(() => [...items].sort((a, b) => b.change - a.change).slice(0, 3), [items]);
  const topLosers = useMemo(() => [...items].sort((a, b) => a.change - b.change).slice(0, 3), [items]);

  return { quotes, history, items, topGainers, topLosers, status, paused, setPaused };
}

const STORAGE_KEY = "stocksim.react.quotes.v1";
const STORAGE_VERSION = 1;

type PersistedState = { quotes: Record<string, Quote>; history: Record<string, HistoryPoint[]> };

function loadPersisted(windowSize: number): PersistedState {
  if (typeof window === "undefined") return { quotes: {}, history: {} };
  const raw = window.localStorage.getItem(STORAGE_KEY);
  if (!raw) return { quotes: {}, history: {} };
  return parsePersisted(raw, windowSize);
}

function parsePersisted(raw: string, windowSize: number): PersistedState {
  try {
    const parsed = JSON.parse(raw);
    if (!parsed || parsed.version !== STORAGE_VERSION) return { quotes: {}, history: {} };
    const quotes = typeof parsed.quotes === "object" && parsed.quotes ? sanitizeQuotes(parsed.quotes) : {};
    const historyInput = typeof parsed.history === "object" && parsed.history ? parsed.history as Record<string, HistoryPoint[]> : {};
    return { quotes, history: trimHistory(historyInput, windowSize) };
  } catch (err) {
    console.warn("Failed to parse persisted quotes", err);
    return { quotes: {}, history: {} };
  }
}

function persistState(state: PersistedState, windowSize: number) {
  if (typeof window === "undefined") return;
  try {
    const payload = {
      version: STORAGE_VERSION,
      quotes: state.quotes,
      history: trimHistory(state.history, windowSize),
    };
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(payload));
  } catch (err) {
    console.warn("Failed to persist quotes history", err);
  }
}

function trimHistory(history: Record<string, HistoryPoint[]>, windowSize: number) {
  const trimmed: Record<string, HistoryPoint[]> = {};
  for (const [symbol, points] of Object.entries(history)) {
    if (Array.isArray(points) && points.length > 0) {
      trimmed[symbol] = points.slice(-windowSize);
    }
  }
  return trimmed;
}

function sanitizeQuotes(raw: Record<string, any>): Record<string, Quote> {
  const result: Record<string, Quote> = {};
  for (const [symbol, value] of Object.entries(raw)) {
    if (!value || typeof value !== "object") continue;
    const price = firstFinite((value as any).price, (value as any).Price);
    const change = firstFinite((value as any).change, (value as any).Change);
    const timeUtc = readString(value, ["timeUtc", "TimeUtc"]) ?? new Date().toISOString();
    if (!Number.isFinite(price) || !Number.isFinite(change)) continue;
    result[symbol] = { symbol, price, change, timeUtc };
  }
  return result;
}

function normalizeQuote(raw: RawQuote, prev?: Quote): Quote | null {
  const symbol = readString(raw, ["symbol", "Symbol"]) ?? prev?.symbol;
  if (!symbol) return null;

  const price = firstFinite(raw.price, raw.Price, raw.last, raw.Last, raw.bid, raw.Bid, raw.ask, raw.Ask, prev?.price);
  if (!Number.isFinite(price)) return null;

  const changeRaw = firstFinite(raw.change, raw.Change);
  const change = Number.isFinite(changeRaw)
    ? Number(changeRaw)
    : prev ? price - prev.price : 0;

  const timeUtc =
    readString(raw, ["timeUtc", "TimeUtc", "ts", "Ts"]) ?? new Date().toISOString();

  return { symbol, price, change, timeUtc };
}

function firstFinite(...values: (number | string | undefined | null)[]) {
  for (const v of values) {
    const n = Number(v);
    if (Number.isFinite(n)) return n;
  }
  return NaN;
}

function readString(obj: any, keys: string[]) {
  for (const k of keys) {
    const value = obj?.[k];
    if (typeof value === "string" && value.length > 0) return value;
  }
  return undefined;
}
