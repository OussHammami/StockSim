import { useMemo } from "react";
import { useQuotes } from "../hooks/useQuotes";
import { Sparkline } from "../components/Sparkline";

export default function LeaderboardPage() {
  const { items, history } = useQuotes();

  const rows = useMemo(() => {
    return items.map(q => {
      const h = history[q.symbol] ?? [];
      const first = h[0]?.p ?? q.price;
      const last = h[h.length - 1]?.p ?? q.price;
      const pct = first ? ((last - first) / first) * 100 : 0;
      return { symbol: q.symbol, price: q.price, pct, series: h.slice(-60) };
    }).sort((a, b) => b.pct - a.pct);
  }, [items, history]);

  return (
    <div style={{ maxWidth: 900, margin: "0 auto", padding: 16 }}>
      <h3>Top Movers (rolling window)</h3>
      <table style={{ width: "100%", borderCollapse: "collapse", tableLayout: "fixed" }}>
        <colgroup suppressHydrationWarning>
          <col style={{ width: 40 }}/>
          <col/><col style={{ width: 110 }}/><col style={{ width: 110 }}/><col style={{ width: 140 }}/>
        </colgroup>
        <thead>
          <tr><th align="left">#</th><th align="left">Symbol</th><th align="right">Price</th><th align="right">Change</th><th align="right">Spark</th></tr>
        </thead>
        <tbody>
          {rows.slice(0, 25).map((r, i) => (
            <tr key={r.symbol}>
              <td>{i + 1}</td>
              <td>{r.symbol}</td>
              <td align="right">{r.price.toFixed(2)}</td>
              <td align="right" style={{ color: r.pct >= 0 ? "var(--success)" : "var(--danger)" }}>
                {r.pct >= 0 ? "+" : ""}{r.pct.toFixed(2)}%
              </td>
              <td align="right">
                <div style={{ display: "inline-flex", justifyContent: "flex-end", width: 120 }}>
                  <Sparkline data={r.series} color={r.pct >= 0 ? "var(--success)" : "var(--danger)"} />
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}