import { useEffect, useMemo, useState } from "react";
import { useQuotes } from "../hooks/useQuotes";
import { useCandles } from "../hooks/useCandles";
import { LiveLineChart } from "../components/LiveLineChart";
import { CandlesChart } from "../components/CandlesChart";

export default function ChartsPage() {
  const { items, history, status, paused, setPaused } = useQuotes();
  const [sel, setSel] = useState("AAPL");
  const [query, setQuery] = useState("");
  const [mode, setMode] = useState<"line" | "candles">("line");
  const [bucketMs, setBucketMs] = useState(5000);
  const [favorites, setFavorites] = useState<string[]>(() => {
    try { return JSON.parse(localStorage.getItem("fav") ?? "[]"); } catch { return []; }
  });

  useEffect(() => { localStorage.setItem("fav", JSON.stringify(favorites)); }, [favorites]);

  const filtered = useMemo(
    () => items.filter(q => q.symbol.toLowerCase().includes(query.toLowerCase())),
    [items, query]
  );

  const hist = history[sel] ?? [];
  const candles = useCandles(hist, bucketMs);

  const toggleFav = (s: string) =>
    setFavorites(prev => prev.includes(s) ? prev.filter(x => x !== s) : [...prev, s]);

  const favSorted = [
    ...filtered.filter(x => favorites.includes(x.symbol)),
    ...filtered.filter(x => !favorites.includes(x.symbol)),
  ];

  return (
    <div style={{ maxWidth: 1200, margin: "0 auto", padding: 16 }}>
      <div style={{ display: "flex", gap: 12, alignItems: "center", marginBottom: 12 }}>
        <input
          placeholder="Search symbol..."
          value={query}
          onChange={e => setQuery(e.target.value)}
          style={{ padding: 6, border: "1px solid var(--border)", borderRadius: 4 }}
        />
        <select value={sel} onChange={e => setSel(e.target.value)} style={{ padding: 6 }}>
          {favSorted.map(q => <option key={q.symbol} value={q.symbol}>{q.symbol}</option>)}
        </select>

        <div style={{ marginLeft: 12 }}>
          <label>
            <input type="radio" name="mode" value="line" checked={mode === "line"} onChange={() => setMode("line")} /> Line
          </label>
          {" "}
          <label>
            <input type="radio" name="mode" value="candles" checked={mode === "candles"} onChange={() => setMode("candles")} /> Candles
          </label>
        </div>

        {mode === "candles" && (
          <select value={bucketMs} onChange={e => setBucketMs(Number(e.target.value))} style={{ marginLeft: 8, padding: 6 }}>
            <option value={1000}>1s</option>
            <option value={5000}>5s</option>
            <option value={10000}>10s</option>
            <option value={30000}>30s</option>
          </select>
        )}

        <button onClick={() => setPaused(!paused)} style={{ marginLeft: "auto", padding: "6px 10px" }}>
          {paused ? "Resume" : "Pause"}
        </button>
        <span style={{ fontSize: 12, color: statusColor(status) }}>● {status}</span>
      </div>

      <div style={{ display: "grid", gridTemplateColumns: "260px 1fr", gap: 16 }}>
        <div className="card" style={{ height: 420, overflow: "auto" }}>
          <table>
            <thead>
              <tr><th align="left">Fav</th><th align="left">Symbol</th><th align="right">Price</th><th align="right">Δ</th></tr>
            </thead>
            <tbody>
              {favSorted.length === 0 && (
                <tr><td colSpan={4} style={{ textAlign: "center", padding: 12, color: "var(--muted)" }}>No quotes yet</td></tr>
              )}
              {favSorted.map(q => (
                <tr
                  key={q.symbol}
                  className={sel === q.symbol ? "row-selected" : ""}
                  style={{ cursor: "pointer" }}
                  onClick={() => setSel(q.symbol)}
                >
                  <td onClick={(e) => { e.stopPropagation(); toggleFav(q.symbol); }}>
                    {favorites.includes(q.symbol) ? "★" : "☆"}
                  </td>
                  <td>{q.symbol}</td>
                  <td align="right">{Number.isFinite(q.price) ? q.price.toFixed(2) : "—"}</td>
                  <td align="right" style={{ color: q.change > 0 ? "var(--success)" : q.change < 0 ? "var(--danger)" : "inherit" }}>
                    {Number.isFinite(q.change) ? (q.change >= 0 ? "+" : "") + q.change.toFixed(2) : "—"}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        <div className="card" style={{ height: 420 }}>
          {mode === "line" ? (
            <LiveLineChart data={hist} />
          ) : (
            <CandlesChart data={candles} />
          )}
        </div>
      </div>
    </div>
  );
}

function statusColor(s: string) {
  switch (s) {
    case "connected": return "var(--success)";
    case "reconnecting": return "orange";
    case "connecting": return "var(--muted)";
    default: return "var(--danger)";
  }
}
