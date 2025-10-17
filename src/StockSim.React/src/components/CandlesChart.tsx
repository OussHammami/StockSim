import { ResponsiveContainer, ComposedChart, XAxis, YAxis, Tooltip, CartesianGrid, Bar } from "recharts";
import type { Candle } from "../hooks/useCandles";

export function CandlesChart({ data }: { data: Candle[] }) {
  if (!data || data.length === 0) return <div style={{ textAlign: "center", paddingTop: 20 }}>No data</div>;
  const min = Math.min(...data.map((d) => d.low));
  const max = Math.max(...data.map((d) => d.high));
  const margin = { top: 8, right: 12, bottom: 8, left: 12 };

  const CandleShape = (props: any) => {
    const { x, width, payload } = props;
    const cx = x + width / 2;
    const isUp = payload.close >= payload.open;
    const color = isUp ? "var(--success)" : "var(--danger)";

    // derive y positions from axis domain
    const scaleY = (val: number) => {
      if (max === min) return 0;
      // use viewBox height if available
      const h = (props?.yAxis?.height as number) || 200;
      return margin.top + (1 - (val - min) / (max - min)) * h;
    };

    const o = scaleY(payload.open);
    const c = scaleY(payload.close);
    const hY = scaleY(payload.high);
    const lY = scaleY(payload.low);
    const bodyTop = Math.min(o, c);
    const bodyBot = Math.max(o, c);
    const bodyH = Math.max(1, bodyBot - bodyTop);
    const bodyW = Math.max(3, Math.min(10, width * 0.6));

    return (
      <g>
        <line x1={cx} x2={cx} y1={hY} y2={lY} stroke={color} strokeWidth={2} />
        <rect x={cx - bodyW / 2} y={bodyTop} width={bodyW} height={bodyH} fill={color} rx={1} />
      </g>
    );
  };

  return (
    <ResponsiveContainer>
      <ComposedChart data={data} margin={margin}>
        <CartesianGrid strokeDasharray="3 3" />
        <XAxis dataKey="t" tickFormatter={(v) => new Date(v).toLocaleTimeString("en-GB", { hour12: false })} minTickGap={24} />
        <YAxis domain={[min, max]} />
        <Tooltip
          wrapperStyle={{ outline: "none" }}
          contentStyle={{ background: "var(--card)", border: "1px solid var(--border)", color: "var(--text)" }}
          labelStyle={{ color: "var(--text)" }}
          itemStyle={{ color: "var(--text)" }}
          formatter={(_, __, p) => {
            const d = p.payload as Candle;
            return [`O:${d.open.toFixed(2)} H:${d.high.toFixed(2)} L:${d.low.toFixed(2)} C:${d.close.toFixed(2)}`, "OHLC"];
          }}
          labelFormatter={(v) => new Date(v as number).toLocaleTimeString("en-GB", { hour12: false })}
        />
        <Bar dataKey="close" fill="transparent" shape={<CandleShape />} />
      </ComposedChart>
    </ResponsiveContainer>
  );
}