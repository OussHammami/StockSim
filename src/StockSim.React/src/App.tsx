import { useEffect, useState } from "react";
import * as signalR from "@microsoft/signalr";
import { LineChart, Line, XAxis, YAxis, Tooltip, ResponsiveContainer } from "recharts";

type Quote = { symbol:string; price:number; change:number; timeUtc:string };

export default function App() {
  const [quotes, setQuotes] = useState<Record<string, Quote>>({});
  const [hist, setHist] = useState<Record<string, { t:number; p:number }[]>>({});
  const [sel, setSel] = useState<string>("AAPL");

  useEffect(() => {
    const base = import.meta.env.VITE_MARKETFEED_URL ?? "http://localhost:8081";
    const hub = new signalR.HubConnectionBuilder()
      .withUrl(`${base}/hubs/quotes`, { skipNegotiation: true, transport: signalR.HttpTransportType.WebSockets })
      .withAutomaticReconnect()
      .build();

    hub.on("quote", (q: Quote) => {
      setQuotes(prev => ({ ...prev, [q.symbol]: q }));
      setHist(h => {
        const a = (h[q.symbol] ?? []).concat({ t: Date.now(), p: q.price });
        return { ...h, [q.symbol]: a.slice(-60) };
      });
    });

    hub.start().catch(console.error);
    return () => { hub.stop(); };
  }, []);

  const items = Object.values(quotes).sort((a,b)=>a.symbol.localeCompare(b.symbol));
  const topUp   = [...items].sort((a,b)=>b.change - a.change).slice(0,3);
  const topDown = [...items].sort((a,b)=>a.change - b.change).slice(0,3);

  return (
    <div style={{ maxWidth: 1100, margin: "32px auto", padding: 16, fontFamily: "system-ui" }}>
      <h2>Live Quotes</h2>

      {/* symbol picker + chart */}
      <div style={{ display:"grid", gridTemplateColumns:"220px 1fr", gap:16, alignItems:"start", marginBottom:24 }}>
        <div>
          <label>Symbol</label>
          <select value={sel} onChange={e=>setSel(e.target.value)} style={{ display:"block", width:"100%", padding:6 }}>
            {items.map(q => <option key={q.symbol} value={q.symbol}>{q.symbol}</option>)}
          </select>

          <div style={{ marginTop:16 }}>
            <h4 style={{ margin:"8px 0" }}>Top Gainers</h4>
            <ul style={{ margin:0, paddingLeft:16 }}>
              {topUp.map(x => <li key={x.symbol} style={{ color:"green" }}>{x.symbol} +{x.change.toFixed(2)}</li>)}
            </ul>
            <h4 style={{ margin:"16px 0 8px" }}>Top Losers</h4>
            <ul style={{ margin:0, paddingLeft:16 }}>
              {topDown.map(x => <li key={x.symbol} style={{ color:"crimson" }}>{x.symbol} {x.change.toFixed(2)}</li>)}
            </ul>
          </div>
        </div>

        <div style={{ height: 260 }}>
          <ResponsiveContainer>
            <LineChart data={hist[sel] ?? []}>
              <XAxis dataKey="t" tickFormatter={(v)=>new Date(v).toLocaleTimeString("en-GB",{hour12:false})} />
              <YAxis domain={["auto","auto"]} />
              <Tooltip labelFormatter={(v)=>new Date(v as number).toLocaleTimeString("en-GB",{hour12:false})} />
              <Line type="monotone" dataKey="p" dot={false} />
            </LineChart>
          </ResponsiveContainer>
        </div>
      </div>

      {/* table */}
      <table style={{ width: "100%", tableLayout: "fixed", borderCollapse: "collapse" }}>
        <thead>
          <tr>
            <th style={{ textAlign: "left", padding: "6px 4px", width: 120 }}>Symbol</th>
            <th style={{ textAlign: "right", padding: "6px 4px", width: 120 }}>Price</th>
            <th style={{ textAlign: "right", padding: "6px 4px", width: 90 }}>Δ</th>
            <th style={{ textAlign: "right", padding: "6px 4px", width: 120 }}>UTC</th>
          </tr>
        </thead>
        <tbody>
          {items.map(q => {
            const sign = q.change >= 0 ? "+" : "";
            const color = q.change > 0 ? "green" : q.change < 0 ? "crimson" : "inherit";
            return (
              <tr key={q.symbol}>
                <td style={{ padding: "4px" }}>{q.symbol}</td>
                <td style={{ padding: "4px", textAlign: "right" }}>{q.price.toLocaleString(undefined,{minimumFractionDigits:2, maximumFractionDigits:2})}</td>
                <td style={{ padding: "4px", textAlign: "right", color }}>{sign}{q.change.toFixed(2)}</td>
                <td style={{ padding: "4px", textAlign: "right" }}>{new Date(q.timeUtc).toLocaleTimeString("en-GB",{hour12:false})}</td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
