import { Link, useLocation } from "react-router-dom";
import { ThemeToggle } from "./ThemeToggle";

export function Navbar() {
  const loc = useLocation();
  const is = (p: string) => (loc.pathname === p ? { color: "var(--text)", fontWeight: 600 } : { color: "var(--muted)" });

  return (
    <div style={{ display: "flex", gap: 16, alignItems: "center", padding: "12px 16px", borderBottom: "1px solid var(--border)" }}>
      <div style={{ fontWeight: 700 }}>StockSim React</div>
      <Link to="/charts" style={is("/charts")}>Charts</Link>
      <Link to="/leaderboard" style={is("/leaderboard")}>Leaderboard</Link>
      <a href="/" style={{ marginLeft: "auto", color: "var(--muted)" }}>Back to Blazor</a>
      <ThemeToggle />
    </div>
  );
}