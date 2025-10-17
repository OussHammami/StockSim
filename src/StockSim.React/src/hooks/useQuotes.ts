import { useEffect, useMemo, useRef, useState } from "react";
import * as signalR from "@microsoft/signalr";

export type Quote = { symbol: string; price: number; change: number; timeUtc: string };
export type HistoryPoint = { t: number; p: number };

type Options = {
  windowSize?: number;
  baseUrl?: string;
  transport?: signalR.HttpTransportType;
  skipNegotiation?: boolean;
};

export function useQuotes(opts: Options = {}) {
  const {
    windowSize = 240,
    baseUrl = import.meta.env.VITE_MARKETFEED_URL ?? "http://localhost:8081",
    transport = signalR.HttpTransportType.WebSockets,
    skipNegotiation = true,
  } = opts;

  const [quotes, setQuotes] = useState<Record<string, Quote>>({});
  const [history, setHistory] = useState<Record<string, HistoryPoint[]>>({});
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

    hub.on("quote", (q: Quote) => {
      if (pausedRef.current) return;
      setQuotes(prev => ({ ...prev, [q.symbol]: q }));
      setHistory(h => {
        const list = (h[q.symbol] ?? []).concat({ t: Date.now(), p: q.price });
        return { ...h, [q.symbol]: list.slice(-windowSize) };
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

  const items = useMemo(() => Object.values(quotes).sort((a, b) => a.symbol.localeCompare(b.symbol)), [quotes]);
  const topGainers = useMemo(() => [...items].sort((a, b) => b.change - a.change).slice(0, 3), [items]);
  const topLosers = useMemo(() => [...items].sort((a, b) => a.change - b.change).slice(0, 3), [items]);

  return { quotes, history, items, topGainers, topLosers, status, paused, setPaused };
}