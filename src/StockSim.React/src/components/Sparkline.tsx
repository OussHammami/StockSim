import { useMemo } from "react";
import { ResponsiveContainer, AreaChart, Area, YAxis } from "recharts";

type SparkPoint = { p: number };

export function Sparkline({
  data,
  color,
  width = 120,
  height = 36,
}: {
  data: SparkPoint[];
  color: string;
  width?: number;
  height?: number;
}) {
  // Prepare a mutable series and bounds for the chart
  const { series, min, max, gid } = useMemo(() => {
    const s: SparkPoint[] = Array.isArray(data) ? [...data] : [];
    const vals = s.map((d) => d.p);
    const mn = s.length ? Math.min(...vals) : 0;
    const mx = s.length ? Math.max(...vals) : 0;
    const uid = `spark-${Math.random().toString(36).slice(2, 9)}`;
    return { series: s, min: mn, max: mx, gid: uid };
  }, [data]);

  if (!series.length) return <div style={{ width, height }} />;

  const pad = Math.max((max - min) * 0.1, 0.01);
  const domain: [number, number] = [min - pad, max + pad];

  // No clipping wrappers; allow overflow if it happens
  return (
    <div style={{ width, height, marginRight: 6 }}>
      <ResponsiveContainer>
        <AreaChart data={series} margin={{ top: 2, right: 0, bottom: 2, left: 0 }}>
          <YAxis hide domain={domain} />
          <defs>
            <linearGradient id={gid} x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor={color} stopOpacity={0.35} />
              <stop offset="100%" stopColor={color} stopOpacity={0} />
            </linearGradient>
          </defs>
          <Area
            type="monotone"
            dataKey="p"
            stroke={color}
            strokeWidth={2}
            strokeLinecap="round"
            isAnimationActive={false}
            fill={`url(#${gid})`}
          />
        </AreaChart>
      </ResponsiveContainer>
    </div>
  );
}