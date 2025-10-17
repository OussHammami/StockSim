import { LineChart, Line, XAxis, YAxis, Tooltip, ResponsiveContainer, CartesianGrid } from "recharts";
import type { HistoryPoint } from "../hooks/useQuotes";

export function LiveLineChart({ data }: { data: HistoryPoint[] }) {
  return (
    <ResponsiveContainer>
      <LineChart data={data} margin={{ top: 8, right: 12, bottom: 8, left: 12 }}>
        <CartesianGrid strokeDasharray="3 3" />
        <XAxis
          dataKey="t"
          tickFormatter={(v) => new Date(v).toLocaleTimeString("en-GB", { hour12: false })}
          minTickGap={24}
        />
        <YAxis domain={["auto", "auto"]} />
        <Tooltip
          wrapperStyle={{ outline: "none" }}
          contentStyle={{ background: "var(--card)", border: "1px solid var(--border)", color: "var(--text)" }}
          labelStyle={{ color: "var(--text)" }}
          itemStyle={{ color: "var(--text)" }}
          formatter={(value: any) => [Number(value).toFixed(2), "Price"]}
          labelFormatter={(v) => new Date(v as number).toLocaleTimeString("en-GB", { hour12: false })}
        />
        <Line type="monotone" dataKey="p" stroke="var(--primary)" dot={false} strokeWidth={2} isAnimationActive={false} />
      </LineChart>
    </ResponsiveContainer>
  );
}